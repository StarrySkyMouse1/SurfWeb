using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Services.IServices;
using SurfWeb.Data.Caching;
using SurfWeb.Utils.Caching;
using SurfWeb.Utils.Common;
using SurfWeb.Utils.Constants;
using SurfWeb.Data.Dtos;
using SurfWeb.Configurations;
using SurfWeb.Repositories;
using SurfWeb.Repositories.Entities;

namespace SurfWeb.Services;

public sealed class RecordService(
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<User> users,
    IBaseRepository<MapTier> mapTiers,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IRecordService
{
    private const float WrGapEpsilon = 0.001f;

    public async Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
        int page, int pageSize, string? filter = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        filter = NormalizeFilter(filter);

        if ((page - 1) * pageSize >= SiteLimits.MaxRecentTotal)
            return ([], SiteLimits.MaxRecentTotal);

        var ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.Cache.RecentRefreshMinutes));
        var snapshot = await cache.GetOrLoadAsync(
            CacheKeys.RecordsRecent,
            ttl,
            LoadRecentSnapshotAsync,
            ct);

        var list = SelectList(snapshot, filter);
        var total = list.Count;
        if (total == 0)
            return ([], 0);

        var take = Math.Min(pageSize, total - (page - 1) * pageSize);
        if (take <= 0)
            return ([], total);

        var items = list
            .Skip((page - 1) * pageSize)
            .Take(take)
            .ToList();
        return (items, total);
    }

    private static IReadOnlyList<RecentRecordDto> SelectList(RecentRecordsSnapshot snapshot, string? filter) =>
        filter switch
        {
            "main" => snapshot.Main,
            "bonus" => snapshot.Bonus,
            "wr" => snapshot.Wr,
            _ => snapshot.All,
        };

    private static string? NormalizeFilter(string? filter) =>
        filter?.Trim().ToLowerInvariant() switch
        {
            "" or "all" => null,
            "main" or "bonus" or "wr" => filter.Trim().ToLowerInvariant(),
            _ => null,
        };

    private static bool IsWr(RecentRecordDto record) =>
        record.GapFromWr is null or <= WrGapEpsilon;

    private async Task<RecentRecordsSnapshot> LoadRecentSnapshotAsync(CancellationToken ct)
    {
        var recentRuns = await playerTimes
            .Where(pt => pt.Auth != null && pt.Date != null)
            .OrderByDescending(pt => pt.Date)
            .Take(SiteLimits.RecentScanBatch)
            .ToListAsync(ct);

        if (recentRuns.Count == 0)
            return new RecentRecordsSnapshot([], [], [], []);

        var allRuns = PickTopPlayerPbByDate(recentRuns);
        var mainRuns = PickTopPlayerPbByDate(recentRuns.Where(pt => pt.Track == 0));
        var bonusRuns = PickTopPlayerPbByDate(recentRuns.Where(pt => pt.Track > 0));

        var maps = recentRuns.Select(pt => pt.Map).Distinct().ToList();
        var minRunTimes = await GetMinRunTimesByMapTrackAsync(maps, ct);
        var wrByRun = minRunTimes
            .GroupBy(x => (x.Map, x.Track))
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));

        var authIds = recentRuns.Select(pt => pt.Auth!.Value).Distinct().ToList();
        var names = await GetNamesByAuthIdsAsync(authIds, ct);
        var tiers = await GetTiersByMapsAsync(maps, ct);

        var all = BuildDtos(allRuns, names, wrByRun, tiers);
        var main = BuildDtos(mainRuns, names, wrByRun, tiers);
        var bonus = BuildDtos(bonusRuns, names, wrByRun, tiers);
        var wr = BuildDtos(DedupeFastestPerPlayerMapTrack(recentRuns), names, wrByRun, tiers)
            .Where(IsWr)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();

        return new RecentRecordsSnapshot(all, main, bonus, wr);
    }

    /// <summary>
    /// 在最近批次内：同一玩家 + 地图 + 赛道只保留该玩家最快一条，再按该条的完成时间降序取 Top 100。
    /// </summary>
    private static List<PlayerTime> PickTopPlayerPbByDate(IEnumerable<PlayerTime> runs) =>
        DedupeFastestPerPlayerMapTrack(runs)
            .OrderByDescending(pt => pt.Date)
            .ThenBy(pt => pt.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();

    private static List<PlayerTime> DedupeFastestPerPlayerMapTrack(IEnumerable<PlayerTime> runs) =>
        runs
            .GroupBy(pt => (Auth: pt.Auth!.Value, pt.Map, pt.Track))
            .Select(g => g
                .OrderBy(pt => pt.Time)
                .ThenByDescending(pt => pt.Date)
                .ThenBy(pt => pt.Id)
                .First())
            .ToList();

    private static List<RecentRecordDto> BuildDtos(
        IReadOnlyList<PlayerTime> runs,
        Dictionary<int, string?> names,
        Dictionary<(string Map, byte Track), float> wrByRun,
        Dictionary<string, int> tiers) =>
        runs
            .Select(pt => ToRecentRecordDto(pt, names, wrByRun, tiers))
            .ToList();

    private async Task<IReadOnlyList<(string Map, byte Track, float MinTime)>> GetMinRunTimesByMapTrackAsync(
        IReadOnlyList<string> maps, CancellationToken ct)
    {
        if (maps.Count == 0) return [];
        var rows = await playerTimes
            .Where(pt => pt.Auth != null && maps.Contains(pt.Map))
            .GroupBy(pt => new { pt.Map, pt.Track, pt.Auth })
            .Select(g => new { g.Key.Map, g.Key.Track, MinTime = g.Min(pt => pt.Time) })
            .ToListAsync(ct);
        return rows.Select(x => (x.Map, x.Track, x.MinTime)).ToList();
    }

    private async Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(
        IReadOnlyList<int> authIds, CancellationToken ct)
    {
        if (authIds.Count == 0) return new Dictionary<int, string?>();
        return await users
            .Where(u => authIds.Contains(u.Auth))
            .ToDictionaryAsync(u => u.Auth, u => u.Name, ct);
    }

    private async Task<Dictionary<string, int>> GetTiersByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct)
    {
        if (mapNames.Count == 0) return new Dictionary<string, int>();
        return await mapTiers
            .Where(mt => mapNames.Contains(mt.Map))
            .ToDictionaryAsync(mt => mt.Map, mt => (int)mt.Tier, ct);
    }

    private static RecentRecordDto ToRecentRecordDto(
        PlayerTime pt,
        Dictionary<int, string?> names,
        Dictionary<(string Map, byte Track), float> wrByMap,
        Dictionary<string, int> tiers)
    {
        names.TryGetValue(pt.Auth!.Value, out var name);
        var key = (pt.Map, pt.Track);
        float? wrTime = wrByMap.TryGetValue(key, out var wr) ? wr : null;
        float? gap = wrTime is not null ? pt.Time - wrTime.Value : null;
        if (gap is <= WrGapEpsilon) gap = null;

        int? tier = tiers.TryGetValue(pt.Map, out var t) ? t : null;

        return new RecentRecordDto(
            pt.Id,
            pt.Auth!.Value,
            name,
            pt.Map,
            pt.Style,
            pt.Track,
            pt.Time,
            TimeFormatter.Format(pt.Time),
            TimeFormatter.FromUnixSeconds(pt.Date),
            wrTime,
            gap,
            Tier: tier);
    }
}
