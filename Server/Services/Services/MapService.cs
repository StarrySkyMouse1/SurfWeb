using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Services.IServices;
using SurfWeb.Utils.Caching;
using SurfWeb.Utils.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Options;
using SurfWeb.Repositories;
using SurfWeb.Core.Models;

namespace SurfWeb.Services;

public sealed class MapService(
    IBaseRepository<MapTier> mapTiers,
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<StageTime> stageTimes,
    IBaseRepository<User> users,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IMapService
{
    private sealed record MapWorldRecord(string Map, float Time, int Auth, int SourceId);

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
        var query = mapTiers.AsQueryable();
        if (tier.HasValue)
            query = query.Where(m => m.Tier == tier.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Map.Contains(search));

        var total = await query.CountAsync(ct);
        var mapList = await query.OrderBy(m => m.Map).Skip(skip).Take(pageSize).ToListAsync(ct);
        var mapNames = mapList.Select(m => m.Map).ToList();
        if (mapNames.Count == 0)
            return new CachedPageList<MapListItemDto>([], total);

        var completionLookup = await GetCompletionCountsByMapsAsync(mapNames, ct);
        var wrList = await GetWorldRecordsByMapsAsync(mapNames, ct);
        var wrByMap = wrList.ToDictionary(w => w.Map);
        var wrAuthIds = wrList.Select(w => w.Auth).Distinct().ToList();
        var wrNames = await GetNamesByAuthIdsAsync(wrAuthIds, ct);

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
        var tier = await mapTiers.FirstOrDefaultAsync(m => m.Map == mapName, ct);
        if (tier is null) return null;

        var completions = await CountDistinctCompletionsAsync(mapName, ct);
        var wr = await GetMainWorldRecordAsync(mapName, ct);
        string? wrName = null;
        if (wr is not null)
            wrName = await GetNameAsync(wr.Value.Auth, ct);

        var bonusTracks = await GetBonusTrackIdsAsync(mapName, ct);

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
        var total = await CountLeaderboardPlayerTimesAsync(mapName, track, ct);
        if (total == 0)
            return new CachedPageList<LeaderboardEntryDto>([], 0);

        var skip = (page - 1) * pageSize;
        var bestPerAuth = await GetLeaderboardPlayerTimePageAsync(mapName, track, skip, pageSize, ct);
        var authIds = bestPerAuth.Select(x => x.Auth).ToList();
        var rows = await GetPlayerTimeRowsForLeaderboardAsync(mapName, track, authIds, ct);
        var names = await GetNamesByAuthIdsAsync(authIds, ct);

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
        var total = await CountLeaderboardStageTimesAsync(mapName, track, stage, ct);
        if (total == 0)
            return new CachedPageList<LeaderboardEntryDto>([], 0);

        var skip = (page - 1) * pageSize;
        var bestPerAuth = await GetLeaderboardStageTimePageAsync(mapName, track, stage, skip, pageSize, ct);
        var authIds = bestPerAuth.Select(x => x.Auth).ToList();
        var rows = await GetStageTimeRowsForLeaderboardAsync(mapName, track, stage, authIds, ct);
        var names = await GetNamesByAuthIdsAsync(authIds, ct);

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

    private Task<int> CountDistinctCompletionsAsync(string mapName, CancellationToken ct) =>
        playerTimes
            .Where(pt => pt.Map == mapName && pt.Track == 0 && pt.Auth != null)
            .Select(pt => pt.Auth)
            .Distinct()
            .CountAsync(ct);

    private async Task<(float Time, int Auth)?> GetMainWorldRecordAsync(string mapName, CancellationToken ct)
    {
        var wr = await playerTimes
            .Where(pt => pt.Map == mapName && pt.Track == 0 && pt.Auth != null)
            .OrderBy(pt => pt.Time)
            .Select(pt => new { pt.Time, Auth = pt.Auth!.Value })
            .FirstOrDefaultAsync(ct);
        return wr is null ? null : (wr.Time, wr.Auth);
    }

    private async Task<IReadOnlyList<byte>> GetBonusTrackIdsAsync(string mapName, CancellationToken ct) =>
        await playerTimes
            .Where(pt => pt.Map == mapName && pt.Track > 0 && pt.Auth != null)
            .Select(pt => pt.Track)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);

    private async Task<Dictionary<string, int>> GetCompletionCountsByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct)
    {
        if (mapNames.Count == 0) return new Dictionary<string, int>();

        var counts = await playerTimes
            .Where(pt => mapNames.Contains(pt.Map) && pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, Count = g.Select(x => x.Auth).Distinct().Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Map, x => x.Count);
    }

    private async Task<IReadOnlyList<MapWorldRecord>> GetWorldRecordsByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct)
    {
        if (mapNames.Count == 0) return [];

        var minTimesQuery = playerTimes
            .Where(pt => mapNames.Contains(pt.Map) && pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(x => x.Time) });

        var wrCandidates = await (
            from pt in playerTimes
            join min in minTimesQuery on pt.Map equals min.Map
            where pt.Track == 0 && pt.Time == min.MinTime && pt.Auth != null
            select new { pt.Map, pt.Time, Auth = pt.Auth!.Value, pt.Id }
        ).ToListAsync(ct);

        return wrCandidates
            .GroupBy(x => x.Map)
            .Select(g =>
            {
                var best = g.OrderBy(x => x.Id).First();
                return new MapWorldRecord(best.Map, best.Time, best.Auth, best.Id);
            })
            .ToList();
    }

    private Task<int> CountLeaderboardPlayerTimesAsync(string mapName, byte track, CancellationToken ct) =>
        playerTimes
            .Where(pt => pt.Map == mapName && pt.Track == track && pt.Auth != null)
            .GroupBy(pt => pt.Auth)
            .CountAsync(ct);

    private async Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardPlayerTimePageAsync(
        string mapName, byte track, int skip, int take, CancellationToken ct)
    {
        var page = await playerTimes
            .Where(pt => pt.Map == mapName && pt.Track == track && pt.Auth != null)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, MinTime = g.Min(x => x.Time) })
            .OrderBy(x => x.MinTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return page.Select(x => (x.Auth, x.MinTime)).ToList();
    }

    private async Task<IReadOnlyList<PlayerTime>> GetPlayerTimeRowsForLeaderboardAsync(
        string mapName, byte track, IReadOnlyList<int> authIds, CancellationToken ct)
    {
        if (authIds.Count == 0) return [];
        return await playerTimes
            .Where(pt => pt.Map == mapName && pt.Track == track
                && pt.Auth != null && authIds.Contains(pt.Auth.Value))
            .ToListAsync(ct);
    }

    private Task<int> CountLeaderboardStageTimesAsync(
        string mapName, byte track, byte stage, CancellationToken ct) =>
        stageTimes
            .Where(st => st.Map == mapName && st.Track == track && st.Stage == stage)
            .GroupBy(st => st.Auth)
            .CountAsync(ct);

    private async Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardStageTimePageAsync(
        string mapName, byte track, byte stage, int skip, int take, CancellationToken ct)
    {
        var page = await stageTimes
            .Where(st => st.Map == mapName && st.Track == track && st.Stage == stage)
            .GroupBy(st => st.Auth)
            .Select(g => new { Auth = g.Key, MinTime = g.Min(x => x.Time) })
            .OrderBy(x => x.MinTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return page.Select(x => (x.Auth, x.MinTime)).ToList();
    }

    private async Task<IReadOnlyList<StageTime>> GetStageTimeRowsForLeaderboardAsync(
        string mapName, byte track, byte stage, IReadOnlyList<int> authIds, CancellationToken ct)
    {
        if (authIds.Count == 0) return [];
        return await stageTimes
            .Where(st => st.Map == mapName && st.Track == track && st.Stage == stage
                && authIds.Contains(st.Auth))
            .ToListAsync(ct);
    }

    private async Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(
        IReadOnlyList<int> authIds, CancellationToken ct)
    {
        if (authIds.Count == 0) return new Dictionary<int, string?>();
        return await users
            .Where(u => authIds.Contains(u.Auth))
            .ToDictionaryAsync(u => u.Auth, u => u.Name, ct);
    }

    private Task<string?> GetNameAsync(int auth, CancellationToken ct) =>
        users.Where(u => u.Auth == auth).Select(u => u.Name).FirstOrDefaultAsync(ct);

    public async Task<int?> GetMapTierByMapNameAsync(string mapName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mapName))
            return null;
        var row = await mapTiers.FirstOrDefaultAsync(m => m.Map == mapName, ct);
        return row?.Tier;
    }
}
