using Microsoft.Extensions.Options;
using SurfWeb.Core.Options;
using SurfWeb.Core.Dtos;
using SurfWeb.ServerStatus.IServices;
using SurfWeb.Utils.Common;

namespace SurfWeb.ServerStatus.Services;

public sealed class ServerStatusService(
    IOptions<SurfWebOptions> options,
    ServerStatusRefresher refresher) : IServerStatusService
{
    public Task RefreshAsync(CancellationToken ct = default) =>
        refresher.RefreshAsync(ct);

    public async Task<IReadOnlyList<ServerStatusDto>> GetStatusesAsync(CancellationToken ct = default)
    {
        var configured = options.Value.Servers
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.Address))
            .ToList();

        if (configured.Count == 0)
            return [];

        var refreshSeconds = Math.Max(5, options.Value.ServerQuery.RefreshSeconds);
        var cacheStale = refresher.LastUpdatedUtc is null
            || DateTime.UtcNow - refresher.LastUpdatedUtc.Value > TimeSpan.FromSeconds(refreshSeconds);

        if (cacheStale)
            await refresher.RefreshAsync(ct);

        var snapshotByIndex = refresher.Snapshot.ToDictionary(s => s.Index);

        var result = new List<ServerStatusDto>(configured.Count);
        for (var i = 0; i < configured.Count; i++)
        {
            var cfg = configured[i];
            snapshotByIndex.TryGetValue(i, out var live);

            var online = live?.Online ?? false;
            var map = online ? live?.Map : live?.Map ?? (string.IsNullOrWhiteSpace(cfg.Map) ? null : cfg.Map.Trim());
            var players = live?.Players ?? cfg.Players ?? 0;
            var maxPlayers = live?.MaxPlayers ?? cfg.MaxPlayers ?? 0;
            if (maxPlayers <= 0 && cfg.MaxPlayers is > 0)
                maxPlayers = cfg.MaxPlayers.Value;

            var onlinePlayers = (live?.OnlinePlayers ?? [])
                .Select(p => new ServerOnlinePlayerDto(
                    p.Name,
                    p.Auth,
                    p.DurationSeconds,
                    TimeFormatter.FormatDuration(p.DurationSeconds)))
                .ToList();

            result.Add(new ServerStatusDto(
                cfg.Name.Trim(),
                cfg.Address.Trim(),
                online,
                map,
                live?.MapTier,
                players,
                maxPlayers,
                string.IsNullOrWhiteSpace(cfg.Note) ? null : cfg.Note.Trim(),
                onlinePlayers));
        }

        return result;
    }
}
