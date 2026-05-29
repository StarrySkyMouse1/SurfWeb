using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Options;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Core.Models;
using SurfWeb.Core.Constants;
using SurfWeb.Utils.Caching;
using SurfWeb.Repositories;
using SurfWeb.Services.IServices;
using SurfWeb.Utils.Common;

namespace SurfWeb.Services;

public sealed class RecordService(
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<StageTime> stageTimes,
    IBaseRepository<User> users,
    IBaseRepository<MapTier> mapTiers,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IRecordService
{
    private const float WrGapEpsilon = 0.001f;

    public async Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
        int page,
        int pageSize,
        RecentRecordFilter filter = RecentRecordFilter.All,
        WrRankingScope wrScope = WrRankingScope.Main,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        if ((page - 1) * pageSize >= SiteLimits.MaxRecentTotal)
            return ([], SiteLimits.MaxRecentTotal);

        var ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.Cache.RecentRefreshMinutes));
        var snapshot = await cache.GetOrLoadAsync(
            CacheKeys.RecordsRecent,
            ttl,
            LoadRecentSnapshotAsync,
            ct);

        var list = SelectList(snapshot, filter, wrScope);
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

    private static IReadOnlyList<RecentRecordDto> SelectList(
        RecentRecordsSnapshot snapshot,
        RecentRecordFilter filter,
        WrRankingScope wrScope) =>
        filter switch
        {
            RecentRecordFilter.Main => snapshot.Main,
            RecentRecordFilter.Stage => snapshot.Stage,
            RecentRecordFilter.Bonus => snapshot.Bonus,
            RecentRecordFilter.Wr => wrScope switch
            {
                WrRankingScope.Bonus => snapshot.WrBonus,
                WrRankingScope.Stage => snapshot.WrStage,
                _ => snapshot.WrMain,
            },
            _ => snapshot.All,
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

        var recentStageRuns = await stageTimes
            .Where(st => st.Date != null)
            .OrderByDescending(st => st.Date)
            .Take(SiteLimits.RecentScanBatch)
            .ToListAsync(ct);

        if (recentRuns.Count == 0 && recentStageRuns.Count == 0)
            return new RecentRecordsSnapshot([], [], [], [], [], [], []);

        var maps = recentRuns.Select(pt => pt.Map)
            .Concat(recentStageRuns.Select(st => st.Map))
            .Distinct()
            .ToList();

        var minRunTimes = await GetMinRunTimesByMapTrackAsync(maps, ct);
        var wrByRun = minRunTimes
            .GroupBy(x => (x.Map, x.Track))
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));

        var minStageTimes = await GetMinStageTimesByMapTrackStageAsync(maps, ct);
        var wrByStage = minStageTimes
            .GroupBy(x => (x.Map, x.Track, x.Stage))
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));

        var authIds = recentRuns
            .Where(pt => pt.Auth != null)
            .Select(pt => pt.Auth!.Value)
            .Concat(recentStageRuns.Select(st => st.Auth))
            .Distinct()
            .ToList();
        var names = await GetNamesByAuthIdsAsync(authIds, ct);
        var tiers = await GetTiersByMapsAsync(maps, ct);

        var allRuns = PickTopPlayerPbByDate(recentRuns);
        var mainRuns = PickTopPlayerPbByDate(recentRuns.Where(pt => pt.Track == 0));
        var bonusRuns = PickTopPlayerPbByDate(recentRuns.Where(pt => pt.Track > 0));
        var stageRuns = PickTopStagePbByDate(recentStageRuns);

        var all = BuildDtos(allRuns, names, wrByRun, tiers);
        var main = BuildDtos(mainRuns, names, wrByRun, tiers);
        var bonus = BuildDtos(bonusRuns, names, wrByRun, tiers);
        var stage = BuildStageDtos(stageRuns, names, wrByStage, tiers);

        var wrCandidates = BuildDtos(DedupeFastestPerPlayerMapTrack(recentRuns), names, wrByRun, tiers)
            .Concat(BuildStageDtos(DedupeFastestPerStage(recentStageRuns), names, wrByStage, tiers))
            .Where(IsWr)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();

        var wrMain = wrCandidates.Where(d => d.Stage is null && d.Track == 0).ToList();
        var wrBonus = wrCandidates.Where(d => d.Stage is null && d.Track > 0).ToList();
        var wrStage = wrCandidates.Where(d => d.Stage is not null).ToList();

        return new RecentRecordsSnapshot(all, main, stage, bonus, wrMain, wrBonus, wrStage);
    }

    private static List<PlayerTime> PickTopPlayerPbByDate(IEnumerable<PlayerTime> runs) =>
        DedupeFastestPerPlayerMapTrack(runs)
            .OrderByDescending(pt => pt.Date)
            .ThenBy(pt => pt.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();

    private static List<StageTime> PickTopStagePbByDate(IEnumerable<StageTime> runs) =>
        DedupeFastestPerStage(runs)
            .OrderByDescending(st => st.Date)
            .ThenBy(st => st.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();

    private static List<PlayerTime> DedupeFastestPerPlayerMapTrack(IEnumerable<PlayerTime> runs) =>
        runs
            .Where(pt => pt.Auth != null)
            .GroupBy(pt => (Auth: pt.Auth!.Value, pt.Map, pt.Track))
            .Select(g => g
                .OrderBy(pt => pt.Time)
                .ThenByDescending(pt => pt.Date)
                .ThenBy(pt => pt.Id)
                .First())
            .ToList();

    private static List<StageTime> DedupeFastestPerStage(IEnumerable<StageTime> runs) =>
        runs
            .GroupBy(st => (st.Auth, st.Map, st.Track, st.Stage))
            .Select(g => g
                .OrderBy(st => st.Time)
                .ThenByDescending(st => st.Date)
                .ThenBy(st => st.Id)
                .First())
            .ToList();

    private static List<RecentRecordDto> BuildDtos(
        IReadOnlyList<PlayerTime> runs,
        Dictionary<int, string?> names,
        Dictionary<(string Map, byte Track), float> wrByRun,
        Dictionary<string, int> tiers) =>
        runs.Select(pt => ToRecentRecordDto(pt, names, wrByRun, tiers)).ToList();

    private static List<RecentRecordDto> BuildStageDtos(
        IReadOnlyList<StageTime> runs,
        Dictionary<int, string?> names,
        Dictionary<(string Map, byte Track, byte Stage), float> wrByStage,
        Dictionary<string, int> tiers) =>
        runs.Select(st => ToRecentRecordDto(st, names, wrByStage, tiers)).ToList();

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

    private async Task<IReadOnlyList<(string Map, byte Track, byte Stage, float MinTime)>> GetMinStageTimesByMapTrackStageAsync(
        IReadOnlyList<string> maps, CancellationToken ct)
    {
        if (maps.Count == 0) return [];
        var rows = await stageTimes
            .Where(st => maps.Contains(st.Map))
            .GroupBy(st => new { st.Map, st.Track, st.Stage, st.Auth })
            .Select(g => new { g.Key.Map, g.Key.Track, g.Key.Stage, MinTime = g.Min(st => st.Time) })
            .ToListAsync(ct);
        return rows.Select(x => (x.Map, x.Track, x.Stage, x.MinTime)).ToList();
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

    private static RecentRecordDto ToRecentRecordDto(
        StageTime st,
        Dictionary<int, string?> names,
        Dictionary<(string Map, byte Track, byte Stage), float> wrByStage,
        Dictionary<string, int> tiers)
    {
        names.TryGetValue(st.Auth, out var name);
        var key = (st.Map, st.Track, st.Stage);
        float? wrTime = wrByStage.TryGetValue(key, out var wr) ? wr : null;
        float? gap = wrTime is not null ? st.Time - wrTime.Value : null;
        if (gap is <= WrGapEpsilon) gap = null;

        int? tier = tiers.TryGetValue(st.Map, out var t) ? t : null;

        return new RecentRecordDto(
            st.Id,
            st.Auth,
            name,
            st.Map,
            st.Style,
            st.Track,
            st.Time,
            TimeFormatter.Format(st.Time),
            TimeFormatter.FromUnixSeconds(st.Date),
            wrTime,
            gap,
            st.Stage,
            tier);
    }
}
