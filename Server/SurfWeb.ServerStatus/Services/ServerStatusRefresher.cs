using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Options;
using SurfWeb.ServerStatus.Models;
using SurfWeb.ServerStatus.Steam;
using SurfWeb.Services.IServices;
using SurfWeb.Utils.Servers;

namespace SurfWeb.ServerStatus.Services;

/// <summary>
/// ?????Steam A2S ???????????? + <see cref="BackgroundService"/>??
/// </summary>
public sealed class ServerStatusRefresher(
    IOptions<SurfWebOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<ServerStatusRefresher> logger) : BackgroundService
{
    private IReadOnlyList<CachedServerStatus> _snapshot = [];

    public IReadOnlyList<CachedServerStatus> Snapshot => _snapshot;

    public DateTimeOffset? LastUpdatedUtc { get; private set; }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var configured = options.Value.Servers
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.Address))
            .ToList();
        if (configured.Count == 0)
        {
            UpdateSnapshot([]);
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
                logger.LogWarning("???????????????{Address}", cfg.Address);
                results.Add(OfflineSnapshot(i, cfg.Map, cfg.Players, cfg.MaxPlayers));
                continue;
            }

            try
            {
                var serverInfo = SteamServerQuery.QueryServer(host, port, timeoutMs);
                IReadOnlyList<SteamServerQuery.SteamPlayerInfo> playerInfos;
                try
                {
                    playerInfos = SteamServerQuery.QueryPlayers(host, port, timeoutMs);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Steam ???????? {Host}:{Port}", host, port);
                    playerInfos = [];
                }

                var mapName = SteamMapNameNormalizer.Normalize(serverInfo.Map);
                int? mapTier = null;
                IReadOnlyDictionary<string, int> authByName = new Dictionary<string, int>();

                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var mapService = scope.ServiceProvider.GetRequiredService<IMapService>();
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                    if (!string.IsNullOrWhiteSpace(mapName))
                        mapTier = await mapService.GetMapTierByMapNameAsync(mapName, ct);

                    var names = playerInfos
                        .Select(p => p.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .ToList();
                    if (names.Count > 0)
                        authByName = await userService.GetAuthsByNamesAsync(names, ct);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Shavit ???????auth / ?? Tier????? Steam ????");
                }

                var maxPlayers = cfg.MaxPlayers ?? (int)serverInfo.MaxPlayers;
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
                logger.LogWarning(ex, "Steam ??????? {Host}:{Port}", host, port);
                results.Add(OfflineSnapshot(i, cfg.Map, cfg.Players, cfg.MaxPlayers));
            }
        }

        UpdateSnapshot(results);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(5, options.Value.ServerQuery.RefreshSeconds);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "???????????");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void UpdateSnapshot(IReadOnlyList<CachedServerStatus> items)
    {
        _snapshot = items;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
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
