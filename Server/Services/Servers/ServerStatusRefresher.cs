using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurfWeb.Services.IServices;
using SurfWeb.Configurations;
using Microsoft.EntityFrameworkCore;
using SurfWeb.Repositories;
using SurfWeb.Repositories.Entities;
using SurfWeb.Utils.Servers;

namespace SurfWeb.Data.Servers;

public sealed class ServerStatusRefresher(
    IOptions<SurfWebOptions> options,
    IServerStatusStore store,
    ISteamServerQuery steam,
    IServiceScopeFactory scopeFactory,
    ILogger<ServerStatusRefresher> logger) : IServerStatusRefresher
{
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var configured = ServerConfigHelper.GetActiveServers(options.Value);
        if (configured.Count == 0)
        {
            store.Update([]);
            return;
        }

        var timeoutMs = Math.Max(500, options.Value.ServerQuery.QueryTimeoutMs);
        var results = new List<CachedServerStatus>(configured.Count);

        for (var i = 0; i < configured.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var cfg = configured[i];

            if (!ServerEndpointParser.TryResolve(cfg.Address, cfg.Host, cfg.Port, out var host, out var port))
            {
                logger.LogWarning("无法解析服务器地址，跳过查询：{Address}", cfg.Address);
                results.Add(OfflineSnapshot(i, cfg.Map, cfg.Players, cfg.MaxPlayers));
                continue;
            }

            try
            {
                var serverInfo = steam.QueryServer(host, port, timeoutMs);
                IReadOnlyList<SteamQueryPlayerInfo> playerInfos;
                try
                {
                    playerInfos = steam.QueryPlayers(host, port, timeoutMs);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Steam 玩家列表查询失败 {Host}:{Port}", host, port);
                    playerInfos = [];
                }

                var mapName = SteamMapNameNormalizer.Normalize(serverInfo.Map);
                int? mapTier = null;
                IReadOnlyDictionary<string, int> authByName = new Dictionary<string, int>();

                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var mapTierRepo = scope.ServiceProvider.GetRequiredService<IBaseRepository<MapTier>>();
                    var userRepo = scope.ServiceProvider.GetRequiredService<IBaseRepository<User>>();

                    if (!string.IsNullOrWhiteSpace(mapName))
                    {
                        var mapTierRow = await mapTierRepo.FirstOrDefaultAsync(m => m.Map == mapName, ct);
                        mapTier = mapTierRow?.Tier;
                    }

                    var names = playerInfos
                        .Select(p => p.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .ToList();
                    if (names.Count > 0)
                        authByName = await GetAuthsByNamesAsync(userRepo, names, ct);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Shavit 数据补充失败（auth / 地图 Tier），仍返回 Steam 基础状态");
                }

                var maxPlayers = cfg.MaxPlayers ?? serverInfo.MaxPlayers;
                if (maxPlayers <= 0)
                    maxPlayers = serverInfo.MaxPlayers;

                var playerCount = playerInfos.Count > 0 ? playerInfos.Count : serverInfo.Players;

                results.Add(new CachedServerStatus
                {
                    Index = i,
                    Online = true,
                    Map = mapName,
                    MapTier = mapTier,
                    Players = playerCount,
                    MaxPlayers = maxPlayers,
                    OnlinePlayers = playerInfos.Select(p => new CachedOnlinePlayer
                    {
                        Name = p.Name,
                        Auth = authByName.TryGetValue(p.Name, out var auth) ? auth : null,
                        DurationSeconds = p.DurationSeconds
                    }).ToList(),
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Steam 服务器查询失败 {Host}:{Port}", host, port);
                results.Add(OfflineSnapshot(i, cfg.Map, cfg.Players, cfg.MaxPlayers));
            }
        }

        store.Update(results);
    }

    private static async Task<Dictionary<string, int>> GetAuthsByNamesAsync(
        IBaseRepository<User> userRepo,
        IReadOnlyList<string> names,
        CancellationToken ct)
    {
        var distinct = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        if (distinct.Count == 0) return new Dictionary<string, int>();

        var matched = await userRepo
            .Where(u => u.Name != null && distinct.Contains(u.Name))
            .Select(u => new { u.Name, u.Auth })
            .ToListAsync(ct);

        return matched
            .Where(u => u.Name is not null)
            .ToDictionary(u => u.Name!, u => u.Auth);
    }

    private static CachedServerStatus OfflineSnapshot(
        int index,
        string? configuredMap,
        int? configuredPlayers,
        int? configuredMaxPlayers) =>
        new()
        {
            Index = index,
            Online = false,
            Map = SteamMapNameNormalizer.Normalize(configuredMap),
            Players = configuredPlayers ?? 0,
            MaxPlayers = configuredMaxPlayers ?? 0,
            OnlinePlayers = [],
        };
}
