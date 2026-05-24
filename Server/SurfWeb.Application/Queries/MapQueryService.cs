using Microsoft.Extensions.Options;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Common;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries.Abstractions;

namespace SurfWeb.Application.Queries;

public sealed class MapQueryService(
    IMapReadRepository maps,
    IUserReadRepository users,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IMapQueryService
{
    public async Task<(IReadOnlyList<MapListItemDto> Items, int Total)> GetMapsAsync(
        int? tier, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var ttl = MapsTtl();
        var key = CacheKeys.MapsList(tier, search, page, pageSize);
        var snapshot = await cache.GetOrLoadAsync(
            key,
            ttl,
            token => LoadMapsPageAsync(tier, search, page, pageSize, token),
            ct);
        return (snapshot.Items, snapshot.Total);
    }

    public Task<MapDetailDto?> GetMapAsync(string mapName, CancellationToken ct = default)
    {
        var ttl = MapsTtl();
        var key = CacheKeys.MapDetail(mapName);
        return cache.GetOrLoadAsync(key, ttl, token => LoadMapDetailAsync(mapName, token), ct);
    }

    public async Task<(IReadOnlyList<LeaderboardEntryDto> Items, int Total)> GetLeaderboardAsync(
        string mapName, byte track, byte? stage, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var ttl = LeaderboardTtl();
        var key = CacheKeys.MapLeaderboard(mapName, track, stage, page, pageSize);
        var snapshot = await cache.GetOrLoadAsync(
            key,
            ttl,
            token => LoadLeaderboardPageAsync(mapName, track, stage, page, pageSize, token),
            ct);
        return (snapshot.Items, snapshot.Total);
    }

    private TimeSpan MapsTtl() =>
        TimeSpan.FromMinutes(Math.Max(1, options.Value.Cache.MapsMinutes));

    private TimeSpan LeaderboardTtl() =>
        TimeSpan.FromSeconds(Math.Max(1, options.Value.Cache.LeaderboardSeconds));

    private async Task<CachedPageList<MapListItemDto>> LoadMapsPageAsync(
        int? tier, string? search, int page, int pageSize, CancellationToken ct)
    {
        var skip = (page - 1) * pageSize;
        var (mapList, total) = await maps.ListMapTiersAsync(tier, search, skip, pageSize, ct);
        var mapNames = mapList.Select(m => m.Map).ToList();
        if (mapNames.Count == 0)
            return new CachedPageList<MapListItemDto>([], total);

        var completionLookup = await maps.GetCompletionCountsByMapsAsync(mapNames, ct);
        var wrList = await maps.GetWorldRecordsByMapsAsync(mapNames, ct);
        var wrByMap = wrList.ToDictionary(w => w.Map);
        var wrAuthIds = wrList.Select(w => w.Auth).Distinct().ToList();
        var wrNames = await users.GetNamesByAuthIdsAsync(wrAuthIds, ct);

        var items = mapList.Select(m =>
        {
            completionLookup.TryGetValue(m.Map, out var completions);
            wrByMap.TryGetValue(m.Map, out var wr);
            string? wrPlayer = null;
            if (wr is not null)
                wrNames.TryGetValue(wr.Auth, out wrPlayer);
            return new MapListItemDto(
                m.Map,
                m.Tier,
                completions,
                wr?.Time,
                wr is null ? null : TimeFormatter.Format(wr.Time),
                wrPlayer);
        }).ToList();

        return new CachedPageList<MapListItemDto>(items, total);
    }

    private async Task<MapDetailDto?> LoadMapDetailAsync(string mapName, CancellationToken ct)
    {
        var tier = await maps.FindMapTierAsync(mapName, ct);
        if (tier is null) return null;

        var completions = await maps.CountDistinctCompletionsAsync(mapName, ct);
        var wr = await maps.GetMainWorldRecordAsync(mapName, ct);
        string? wrName = null;
        if (wr is not null)
            wrName = await users.GetNameAsync(wr.Value.Auth, ct);

        var bonusTracks = await maps.GetBonusTrackIdsAsync(mapName, ct);

        return new MapDetailDto(
            tier.Map,
            tier.Tier,
            tier.Maxvelocity,
            completions,
            wr?.Time,
            wr is null ? null : TimeFormatter.Format(wr.Value.Time),
            wrName,
            wr?.Auth,
            bonusTracks);
    }

    private Task<CachedPageList<LeaderboardEntryDto>> LoadLeaderboardPageAsync(
        string mapName,
        byte track,
        byte? stage,
        int page,
        int pageSize,
        CancellationToken ct) =>
        stage.HasValue
            ? LoadStageLeaderboardSnapshotAsync(mapName, track, stage.Value, page, pageSize, ct)
            : LoadPlayerTimeLeaderboardSnapshotAsync(mapName, track, page, pageSize, ct);

    private async Task<CachedPageList<LeaderboardEntryDto>> LoadPlayerTimeLeaderboardSnapshotAsync(
        string mapName, byte track, int page, int pageSize, CancellationToken ct)
    {
        var total = await maps.CountLeaderboardPlayerTimesAsync(mapName, track, ct);
        if (total == 0)
            return new CachedPageList<LeaderboardEntryDto>([], 0);

        var skip = (page - 1) * pageSize;
        var bestPerAuth = await maps.GetLeaderboardPlayerTimePageAsync(mapName, track, skip, pageSize, ct);
        var authIds = bestPerAuth.Select(x => x.Auth).ToList();
        var rows = await maps.GetPlayerTimeRowsForLeaderboardAsync(mapName, track, authIds, ct);
        var names = await users.GetNamesByAuthIdsAsync(authIds, ct);

        var items = bestPerAuth
            .Select((best, index) =>
            {
                var row = rows
                    .Where(r => r.Auth == best.Auth && r.Time == best.MinTime)
                    .OrderBy(r => r.Id)
                    .First();
                names.TryGetValue(best.Auth, out var name);
                return ToLeaderboardEntry(best.Auth, name, row.Time, row.Sync, row.Jumps, row.Date, skip + index + 1);
            })
            .ToList();

        return new CachedPageList<LeaderboardEntryDto>(items, total);
    }

    private async Task<CachedPageList<LeaderboardEntryDto>> LoadStageLeaderboardSnapshotAsync(
        string mapName, byte track, byte stage, int page, int pageSize, CancellationToken ct)
    {
        var total = await maps.CountLeaderboardStageTimesAsync(mapName, track, stage, ct);
        if (total == 0)
            return new CachedPageList<LeaderboardEntryDto>([], 0);

        var skip = (page - 1) * pageSize;
        var bestPerAuth = await maps.GetLeaderboardStageTimePageAsync(mapName, track, stage, skip, pageSize, ct);
        var authIds = bestPerAuth.Select(x => x.Auth).ToList();
        var rows = await maps.GetStageTimeRowsForLeaderboardAsync(mapName, track, stage, authIds, ct);
        var names = await users.GetNamesByAuthIdsAsync(authIds, ct);

        var items = bestPerAuth
            .Select((best, index) =>
            {
                var row = rows
                    .Where(r => r.Auth == best.Auth && r.Time == best.MinTime)
                    .OrderBy(r => r.Id)
                    .First();
                names.TryGetValue(best.Auth, out var name);
                return ToLeaderboardEntry(best.Auth, name, row.Time, row.Sync, row.Jumps, row.Date, skip + index + 1);
            })
            .ToList();

        return new CachedPageList<LeaderboardEntryDto>(items, total);
    }

    private static LeaderboardEntryDto ToLeaderboardEntry(
        int auth, string? name, float time, float? sync, int? jumps, int? date, int rank) =>
        new(rank, auth, name, time, TimeFormatter.Format(time), sync, jumps, TimeFormatter.FromUnixSeconds(date));
}
