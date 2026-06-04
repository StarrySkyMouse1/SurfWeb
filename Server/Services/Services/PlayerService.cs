using Microsoft.EntityFrameworkCore;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Core.Models;
using SurfWeb.Repositories;
using SurfWeb.Services.IServices;
using SurfWeb.Utils.Common;

namespace SurfWeb.Services;

public sealed class PlayerService(
    IBaseRepository<User> users,
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<StageTime> stageTimes,
    IBaseRepository<MapTier> mapTiers) : IPlayerService
{
    private const float WrGapEpsilon = 0.001f;
    private const int MaxDisplayTier = 8;
    private const string WrChartsPrimaryTitle = "今年 WR 达成";
    private const string RecentChartsPrimaryTitle = "今年完成次数";

    public async Task<PlayerSummaryDto?> GetPlayerAsync(int auth, CancellationToken ct = default)
    {
        var user = await users.FirstOrDefaultAsync(u => u.Auth == auth, ct);
        if (user is null) return null;

        var mainCompletions = await CountMainCompletionsAsync(auth, ct);
        var bonusCompletions = await CountBonusCompletionsAsync(auth, ct);
        var mainWr = await CountPlayerMainWrAsync(auth, ct);
        var stageWr = await CountPlayerStageWrAsync(auth, ct);
        var bonusWr = await CountPlayerBonusWrAsync(auth, ct);
        var wrTotal = mainWr + stageWr + bonusWr;

        return new PlayerSummaryDto(
            user.Auth,
            user.Name,
            user.Points,
            await CountAheadByPointsAsync(user.Points, auth, ct) + 1,
            user.Playtime,
            await CountAheadByPlaytimeAsync(user.Playtime, auth, ct) + 1,
            mainCompletions,
            await CountAheadByMainCompletionsAsync(mainCompletions, auth, ct) + 1,
            bonusCompletions,
            await CountAheadByBonusCompletionsAsync(bonusCompletions, auth, ct) + 1,
            wrTotal,
            await CountAheadByWrTotalAsync(wrTotal, auth, ct) + 1,
            mainWr,
            await CountAheadByMainWrAsync(mainWr, auth, ct) + 1,
            stageWr,
            await CountAheadByStageWrAsync(stageWr, auth, ct) + 1,
            bonusWr,
            await CountAheadByBonusWrAsync(bonusWr, auth, ct) + 1);
    }

    public async Task<(PlayerRecordsPageDto Page, int Total)?> GetPlayerRecordsAsync(
        int auth,
        PlayerRecordCategory category,
        PlayerRecordScope scope,
        int page,
        int pageSize,
        int? tier = null,
        CancellationToken ct = default)
    {
        if (!await users.AnyAsync(u => u.Auth == auth, ct))
            return null;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var incompleteTier = category == PlayerRecordCategory.Incomplete ? tier : null;
        var (allRecords, charts) = await BuildRecordsAndChartsAsync(auth, category, scope, incompleteTier, ct);
        var total = allRecords.Count;
        var skip = (page - 1) * pageSize;
        var pageItems = allRecords.Skip(skip).Take(pageSize).ToList();
        return (new PlayerRecordsPageDto(pageItems, charts), total);
    }

    private async Task<(List<PlayerRecordDto> Records, PlayerChartsDto Charts)> BuildRecordsAndChartsAsync(
        int auth,
        PlayerRecordCategory category,
        PlayerRecordScope scope,
        int? incompleteTier,
        CancellationToken ct)
    {
        var tierByMap = await LoadTierByMapAsync(ct);

        return (category, scope) switch
        {
            (PlayerRecordCategory.Recent, PlayerRecordScope.Main) => await BuildRecentMainAsync(auth, tierByMap, ct),
            (PlayerRecordCategory.Recent, PlayerRecordScope.Stage) => await BuildRecentStageAsync(auth, tierByMap, ct),
            (PlayerRecordCategory.Recent, PlayerRecordScope.Bonus) => await BuildRecentBonusAsync(auth, tierByMap, ct),
            (PlayerRecordCategory.Wr, PlayerRecordScope.Main) => await BuildWrMainAsync(auth, tierByMap, ct),
            (PlayerRecordCategory.Wr, PlayerRecordScope.Stage) => await BuildWrStageAsync(auth, tierByMap, ct),
            (PlayerRecordCategory.Wr, PlayerRecordScope.Bonus) => await BuildWrBonusAsync(auth, tierByMap, ct),
            (PlayerRecordCategory.Incomplete, PlayerRecordScope.Main) => await BuildIncompleteMainAsync(auth, tierByMap, incompleteTier, ct),
            (PlayerRecordCategory.Incomplete, PlayerRecordScope.Stage) => await BuildIncompleteStageAsync(auth, tierByMap, incompleteTier, ct),
            (PlayerRecordCategory.Incomplete, PlayerRecordScope.Bonus) => await BuildIncompleteBonusAsync(auth, tierByMap, incompleteTier, ct),
            _ => ([], EmptyCharts("记录", "按 Tier")),
        };
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildRecentMainAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        CancellationToken ct)
    {
        var rows = await playerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .OrderByDescending(pt => pt.Date ?? 0)
            .ThenByDescending(pt => pt.Id)
            .ToListAsync(ct);

        var maps = rows.Select(pt => pt.Map).Distinct().ToList();
        var wrByMapTrack = await GetWrByMapTrackAsync(maps, ct);
        var records = rows
            .Select(pt => ToRecentRecord(pt.Map, tierByMap, pt.Track, null, pt.Time, pt.Sync, pt.Date, wrByMapTrack))
            .ToList();
        var charts = BuildRecentCharts(rows, tierByMap, RecentChartsPrimaryTitle, "主线完成 · 按 Tier", r => r.Track == 0);
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildRecentBonusAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        CancellationToken ct)
    {
        var rows = await playerTimes
            .Where(pt => pt.Auth == auth && pt.Track > 0)
            .OrderByDescending(pt => pt.Date ?? 0)
            .ThenByDescending(pt => pt.Id)
            .ToListAsync(ct);

        var maps = rows.Select(pt => pt.Map).Distinct().ToList();
        var wrByMapTrack = await GetWrByMapTrackAsync(maps, ct);
        var records = rows
            .Select(pt => ToRecentRecord(pt.Map, tierByMap, pt.Track, null, pt.Time, pt.Sync, pt.Date, wrByMapTrack))
            .ToList();
        var charts = BuildRecentCharts(rows, tierByMap, RecentChartsPrimaryTitle, "奖励完成 · 按 Tier", _ => true);
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildRecentStageAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        CancellationToken ct)
    {
        var rows = await stageTimes
            .Where(st => st.Auth == auth)
            .OrderByDescending(st => st.Date ?? 0)
            .ThenByDescending(st => st.Id)
            .ToListAsync(ct);

        var maps = rows.Select(st => st.Map).Distinct().ToList();
        var wrByStage = await GetWrByMapTrackStageAsync(maps, ct);
        var records = rows
            .Select(st => ToRecentRecord(st.Map, tierByMap, st.Track, st.Stage, st.Time, st.Sync, st.Date, null, wrByStage))
            .ToList();
        var pseudo = rows.Select(st => new PlayerTime
        {
            Map = st.Map,
            Track = st.Track,
            Date = st.Date,
            Id = st.Id,
        }).ToList();
        var charts = BuildRecentCharts(pseudo, tierByMap, RecentChartsPrimaryTitle, "阶段记录 · 按 Tier", _ => true);
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildWrMainAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        CancellationToken ct)
    {
        var wrRows = await ListPlayerMainWrTimesAsync(auth, ct);
        var records = wrRows
            .OrderByDescending(pt => pt.Date ?? 0)
            .ThenByDescending(pt => pt.Id)
            .Select(pt => ToWrRecord(pt.Map, tierByMap, pt.Track, null, pt.Time, pt.Date))
            .ToList();
        var charts = BuildWrCharts(wrRows, tierByMap, WrChartsPrimaryTitle, "主线 WR · 按 Tier");
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildWrBonusAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        CancellationToken ct)
    {
        var wrRows = await ListPlayerBonusWrTimesAsync(auth, ct);
        var records = wrRows
            .OrderByDescending(pt => pt.Date ?? 0)
            .ThenByDescending(pt => pt.Id)
            .Select(pt => ToWrRecord(pt.Map, tierByMap, pt.Track, null, pt.Time, pt.Date))
            .ToList();
        var charts = BuildWrCharts(wrRows, tierByMap, WrChartsPrimaryTitle, "奖励 WR · 按 Tier");
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildWrStageAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        CancellationToken ct)
    {
        var wrRows = await ListPlayerStageWrTimesAsync(auth, ct);
        var records = wrRows
            .OrderByDescending(st => st.Date ?? 0)
            .ThenByDescending(st => st.Id)
            .Select(st => ToWrRecord(st.Map, tierByMap, st.Track, st.Stage, st.Time, st.Date))
            .ToList();
        var charts = BuildWrStageCharts(wrRows, tierByMap);
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildIncompleteMainAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        int? tier,
        CancellationToken ct)
    {
        var completed = await playerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .Select(pt => pt.Map)
            .Distinct()
            .ToListAsync(ct);
        var completedSet = completed.ToHashSet(StringComparer.Ordinal);

        var maps = await mapTiers.OrderBy(m => m.Tier).ThenBy(m => m.Map).ToListAsync(ct);
        if (tier is int t)
            maps = maps.Where(m => m.Tier == t).ToList();

        var incompleteMaps = maps.Where(m => !completedSet.Contains(m.Map)).ToList();
        var mainCompleted = maps.Count - incompleteMaps.Count;

        var records = incompleteMaps
            .Select(m => new PlayerRecordDto(
                m.Map,
                m.Tier,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "未完成 · 无主线成绩"))
            .ToList();

        var charts = BuildIncompleteCompareCharts(
            mainCompleted,
            incompleteMaps.Count,
            incompleteMaps.Select(m => m.Tier));
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildIncompleteBonusAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        int? tier,
        CancellationToken ct)
    {
        var mapNames = await mapTiers.Select(m => m.Map).ToListAsync(ct);
        var mapSet = mapNames.ToHashSet(StringComparer.Ordinal);

        var globalTracks = await playerTimes
            .Where(pt => pt.Track > 0 && mapNames.Contains(pt.Map))
            .GroupBy(pt => new { pt.Map, pt.Track })
            .Select(g => g.Key)
            .ToListAsync(ct);

        var playerTracks = await playerTimes
            .Where(pt => pt.Auth == auth && pt.Track > 0)
            .GroupBy(pt => new { pt.Map, pt.Track })
            .Select(g => g.Key)
            .ToListAsync(ct);
        var playerSet = playerTracks
            .Select(k => $"{k.Map}\0{k.Track}")
            .ToHashSet(StringComparer.Ordinal);

        var inScopeTracks = globalTracks
            .Where(k => mapSet.Contains(k.Map))
            .Where(k => tier is not int t || tierByMap.GetValueOrDefault(k.Map, 0) == t)
            .ToList();

        var missing = inScopeTracks
            .Where(k => !playerSet.Contains($"{k.Map}\0{k.Track}"))
            .OrderBy(k => tierByMap.GetValueOrDefault(k.Map, 99))
            .ThenBy(k => k.Map)
            .ThenBy(k => k.Track)
            .ToList();

        var bonusCompleted = inScopeTracks.Count - missing.Count;

        var records = missing
            .Select(k => new PlayerRecordDto(
                k.Map,
                tierByMap.GetValueOrDefault(k.Map),
                k.Track,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "未完成 · 该赛道无成绩"))
            .ToList();

        var charts = BuildIncompleteCompareCharts(
            bonusCompleted,
            missing.Count,
            missing.Select(k => tierByMap.GetValueOrDefault(k.Map, 0)));
        return (records, charts);
    }

    private async Task<(List<PlayerRecordDto>, PlayerChartsDto)> BuildIncompleteStageAsync(
        int auth,
        Dictionary<string, int> tierByMap,
        int? tier,
        CancellationToken ct)
    {
        var mapNames = await mapTiers.Select(m => m.Map).ToListAsync(ct);

        var globalStages = await stageTimes
            .Where(st => mapNames.Contains(st.Map))
            .GroupBy(st => new { st.Map, st.Track, st.Stage })
            .Select(g => g.Key)
            .ToListAsync(ct);

        var playerStages = await stageTimes
            .Where(st => st.Auth == auth)
            .GroupBy(st => new { st.Map, st.Track, st.Stage })
            .Select(g => g.Key)
            .ToListAsync(ct);
        var playerSet = playerStages
            .Select(k => $"{k.Map}\0{k.Track}\0{k.Stage}")
            .ToHashSet(StringComparer.Ordinal);

        var inScopeStages = globalStages
            .Where(k => tier is not int t || tierByMap.GetValueOrDefault(k.Map, 0) == t)
            .ToList();

        var missing = inScopeStages
            .Where(k => !playerSet.Contains($"{k.Map}\0{k.Track}\0{k.Stage}"))
            .OrderBy(k => tierByMap.GetValueOrDefault(k.Map, 99))
            .ThenBy(k => k.Map)
            .ThenBy(k => k.Track)
            .ThenBy(k => k.Stage)
            .ToList();

        var stageCompleted = inScopeStages.Count - missing.Count;

        var records = missing
            .Select(k => new PlayerRecordDto(
                k.Map,
                tierByMap.GetValueOrDefault(k.Map),
                k.Track,
                k.Stage,
                null,
                null,
                null,
                null,
                null,
                null,
                "未完成 · 该阶段无成绩"))
            .ToList();

        var charts = BuildIncompleteCompareCharts(
            stageCompleted,
            missing.Count,
            missing.Select(k => tierByMap.GetValueOrDefault(k.Map, 0)));
        return (records, charts);
    }

    private static PlayerRecordDto ToRecentRecord(
        string map,
        Dictionary<string, int> tierByMap,
        byte track,
        byte? stage,
        float time,
        float? sync,
        int? dateUnix,
        Dictionary<(string Map, byte Track), float>? wrByMapTrack = null,
        Dictionary<(string Map, byte Track, byte Stage), float>? wrByStage = null)
    {
        float? wrTime = null;
        float? gap = null;
        if (stage is not null && wrByStage is not null
            && wrByStage.TryGetValue((map, track, stage.Value), out var stageWr))
        {
            wrTime = stageWr;
            gap = time - stageWr;
        }
        else if (wrByMapTrack is not null && wrByMapTrack.TryGetValue((map, track), out var runWr))
        {
            wrTime = runWr;
            gap = time - runWr;
        }

        if (gap is not null && gap <= WrGapEpsilon)
            gap = 0;

        return new(
            map,
            tierByMap.GetValueOrDefault(map),
            track,
            stage,
            time,
            TimeFormatter.Format(time),
            sync,
            TimeFormatter.FromUnixSeconds(dateUnix),
            wrTime,
            gap,
            null);
    }

    private static PlayerRecordDto ToWrRecord(
        string map,
        Dictionary<string, int> tierByMap,
        byte track,
        byte? stage,
        float time,
        int? dateUnix) =>
        new(
            map,
            tierByMap.GetValueOrDefault(map),
            track,
            stage,
            time,
            TimeFormatter.Format(time),
            null,
            TimeFormatter.FromUnixSeconds(dateUnix),
            null,
            null,
            null);

    private async Task<Dictionary<(string Map, byte Track), float>> GetWrByMapTrackAsync(
        IReadOnlyList<string> maps,
        CancellationToken ct)
    {
        if (maps.Count == 0) return new Dictionary<(string Map, byte Track), float>();

        var rows = await playerTimes
            .Where(pt => pt.Auth != null && maps.Contains(pt.Map))
            .GroupBy(pt => new { pt.Map, pt.Track, pt.Auth })
            .Select(g => new { g.Key.Map, g.Key.Track, MinTime = g.Min(pt => pt.Time) })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => (x.Map, x.Track))
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));
    }

    private async Task<Dictionary<(string Map, byte Track, byte Stage), float>> GetWrByMapTrackStageAsync(
        IReadOnlyList<string> maps,
        CancellationToken ct)
    {
        if (maps.Count == 0) return new Dictionary<(string Map, byte Track, byte Stage), float>();

        var rows = await stageTimes
            .Where(st => maps.Contains(st.Map))
            .GroupBy(st => new { st.Map, st.Track, st.Stage, st.Auth })
            .Select(g => new { g.Key.Map, g.Key.Track, g.Key.Stage, MinTime = g.Min(st => st.Time) })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => (x.Map, x.Track, x.Stage))
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));
    }

    private static PlayerChartsDto BuildRecentCharts(
        IReadOnlyList<PlayerTime> rows,
        Dictionary<string, int> tierByMap,
        string primaryTitle,
        string tierTitle,
        Func<PlayerTime, bool> tierInclude)
    {
        var year = DateTime.UtcNow.Year;
        var primary = new int[12];
        foreach (var row in rows)
        {
            if (row.Date is null or <= 0) continue;
            var dt = DateTimeOffset.FromUnixTimeSeconds(row.Date.Value).UtcDateTime;
            if (dt.Year != year) continue;
            primary[dt.Month - 1]++;
        }

        var primaryBars = Enumerable.Range(1, 12)
            .Select(m => new PlayerChartBarDto($"{m}月", primary[m - 1]))
            .ToList();

        var tierCounts = new Dictionary<int, int>();
        foreach (var row in rows.Where(tierInclude))
        {
            if (!tierByMap.TryGetValue(row.Map, out var tier)) continue;
            tierCounts[tier] = tierCounts.GetValueOrDefault(tier) + 1;
        }

        var tierBars = BuildTierBarDtos(tierCounts);

        var topTier = tierCounts.Count > 0
            ? $"T{tierCounts.OrderByDescending(kv => kv.Value).First().Key}"
            : null;

        return new PlayerChartsDto(
            primaryTitle,
            tierTitle,
            primaryBars,
            tierBars,
            rows.Count,
            topTier,
            $"本范围累计 {rows.Count}",
            topTier is null ? null : $"最多 {topTier}");
    }

    private static PlayerChartsDto BuildWrCharts(
        IReadOnlyList<PlayerTime> wrRows,
        Dictionary<string, int> tierByMap,
        string primaryTitle,
        string tierTitle)
    {
        var year = DateTime.UtcNow.Year;
        var primary = new int[12];
        foreach (var row in wrRows)
        {
            if (row.Date is null or <= 0) continue;
            var dt = DateTimeOffset.FromUnixTimeSeconds(row.Date.Value).UtcDateTime;
            if (dt.Year != year) continue;
            primary[dt.Month - 1]++;
        }

        var primaryBars = Enumerable.Range(1, 12)
            .Select(m => new PlayerChartBarDto($"{m}月", primary[m - 1]))
            .ToList();

        var tierCounts = new Dictionary<int, int>();
        foreach (var row in wrRows)
        {
            if (!tierByMap.TryGetValue(row.Map, out var tier)) continue;
            tierCounts[tier] = tierCounts.GetValueOrDefault(tier) + 1;
        }

        var tierBars = BuildTierBarDtos(tierCounts);

        return new PlayerChartsDto(
            primaryTitle,
            tierTitle,
            primaryBars,
            tierBars,
            wrRows.Count,
            null,
            $"当前范围 WR {wrRows.Count}",
            null);
    }

    private static PlayerChartsDto BuildWrStageCharts(
        IReadOnlyList<StageTime> wrRows,
        Dictionary<string, int> tierByMap)
    {
        var pseudo = wrRows.Select(st => new PlayerTime { Map = st.Map, Date = st.Date, Id = st.Id }).ToList();
        return BuildWrCharts(pseudo, tierByMap, WrChartsPrimaryTitle, "阶段 WR · 按 Tier");
    }

    private static PlayerChartsDto BuildIncompleteCompareCharts(
        int completed,
        int incomplete,
        IEnumerable<int> incompleteTierValues)
    {
        var primaryBars = new[]
        {
            new PlayerChartBarDto("已完成", completed),
            new PlayerChartBarDto("未完成", incomplete),
        };

        var tierCounts = incompleteTierValues
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        var tierBars = BuildTierBarDtos(tierCounts);

        var total = completed + incomplete;
        var pct = total > 0 ? (int)Math.Round(100.0 * completed / total) : 0;

        return new PlayerChartsDto(
            "全图库完成率",
            "未完成 · 按 Tier",
            primaryBars,
            tierBars,
            incomplete,
            null,
            $"完成 {pct}%",
            $"剩余 {incomplete}");
    }

    private static PlayerChartsDto EmptyCharts(string primary, string tier) =>
        new(primary, tier, [], [], 0, null, null, null);

    private static List<PlayerChartBarDto> BuildTierBarDtos(IReadOnlyDictionary<int, int> tierCounts) =>
        Enumerable.Range(0, MaxDisplayTier + 1)
            .Select(t => new PlayerChartBarDto($"T{t}", tierCounts.TryGetValue(t, out var c) ? c : 0))
            .ToList();

    private async Task<Dictionary<string, int>> LoadTierByMapAsync(CancellationToken ct)
    {
        var rows = await mapTiers.ToListFromDbAsync(ct);
        return rows.ToDictionary(m => m.Map, m => m.Tier);
    }

    private async Task<List<PlayerTime>> ListPlayerMainWrTimesAsync(int auth, CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(x => x.Time) });

        return await (
            from pt in playerTimes
            join min in minTimesQuery on pt.Map equals min.Map
            where pt.Auth == auth && pt.Track == 0 && pt.Time == min.MinTime
            select pt).ToListAsync(ct);
    }

    private async Task<List<PlayerTime>> ListPlayerBonusWrTimesAsync(int auth, CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track > 0 && pt.Auth != null)
            .GroupBy(pt => new { pt.Map, pt.Track })
            .Select(g => new { g.Key.Map, g.Key.Track, MinTime = g.Min(x => x.Time) });

        return await (
            from pt in playerTimes
            join min in minTimesQuery on new { pt.Map, pt.Track } equals new { min.Map, min.Track }
            where pt.Auth == auth && pt.Track > 0 && pt.Time == min.MinTime
            select pt).ToListAsync(ct);
    }

    private async Task<List<StageTime>> ListPlayerStageWrTimesAsync(int auth, CancellationToken ct)
    {
        var minTimesQuery = stageTimes
            .GroupBy(st => new { st.Map, st.Track, st.Stage })
            .Select(g => new
            {
                g.Key.Map,
                g.Key.Track,
                g.Key.Stage,
                MinTime = g.Min(x => x.Time),
            });

        return await (
            from st in stageTimes
            join min in minTimesQuery
                on new { st.Map, st.Track, st.Stage }
                equals new { min.Map, min.Track, min.Stage }
            where st.Auth == auth && st.Time == min.MinTime
            select st).ToListAsync(ct);
    }

    private Task<int> CountMainCompletionsAsync(int auth, CancellationToken ct) =>
        playerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .Select(pt => pt.Map)
            .Distinct()
            .CountAsync(ct);

    private async Task<int> CountBonusCompletionsAsync(int auth, CancellationToken ct)
    {
        var keys = await playerTimes
            .Where(pt => pt.Auth == auth && pt.Track > 0)
            .GroupBy(pt => new { pt.Map, pt.Track })
            .Select(g => 1)
            .CountAsync(ct);
        return keys;
    }

    private async Task<int> CountPlayerMainWrAsync(int auth, CancellationToken ct) =>
        (await ListPlayerMainWrTimesAsync(auth, ct)).Count;

    private async Task<int> CountPlayerBonusWrAsync(int auth, CancellationToken ct) =>
        (await ListPlayerBonusWrTimesAsync(auth, ct)).Count;

    private async Task<int> CountPlayerStageWrAsync(int auth, CancellationToken ct) =>
        (await ListPlayerStageWrTimesAsync(auth, ct)).Count;

    private Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct) =>
        users.CountAsync(
            u => u.Auth != auth && (u.Points > points || (u.Points == points && u.Auth < auth)),
            ct);

    private Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct) =>
        users.CountAsync(
            u => u.Auth != auth && (u.Playtime > playtime || (u.Playtime == playtime && u.Auth < auth)),
            ct);

    private Task<int> CountAheadByMainCompletionsAsync(int completions, int auth, CancellationToken ct)
    {
        var ranked = playerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, Count = g.Select(x => x.Map).Distinct().Count() });
        return ranked.CountAsync(
            x => x.Count > completions || (x.Count == completions && x.Auth < auth),
            ct);
    }

    private async Task<int> CountAheadByBonusCompletionsAsync(int completions, int auth, CancellationToken ct)
    {
        var authPerMapTrack = await playerTimes
            .Where(pt => pt.Auth != null && pt.Track > 0)
            .GroupBy(pt => new { Auth = pt.Auth!.Value, pt.Map, pt.Track })
            .Select(g => g.Key.Auth)
            .ToListAsync(ct);

        var counts = authPerMapTrack
            .GroupBy(a => a)
            .Select(g => new { Auth = g.Key, Count = g.Count() })
            .ToList();

        return counts.Count(x => x.Count > completions || (x.Count == completions && x.Auth < auth));
    }

    private async Task<int> CountAheadByMainWrAsync(int wrCount, int auth, CancellationToken ct)
    {
        var counts = await CountWrByAuthMainAsync(ct);
        return CountAhead(wrCount, auth, counts);
    }

    private async Task<int> CountAheadByBonusWrAsync(int wrCount, int auth, CancellationToken ct)
    {
        var counts = await CountWrByAuthBonusAsync(ct);
        return CountAhead(wrCount, auth, counts);
    }

    private async Task<int> CountAheadByStageWrAsync(int wrCount, int auth, CancellationToken ct)
    {
        var counts = await CountWrByAuthStageAsync(ct);
        return CountAhead(wrCount, auth, counts);
    }

    private async Task<int> CountAheadByWrTotalAsync(int wrTotal, int auth, CancellationToken ct)
    {
        var main = await CountWrByAuthMainAsync(ct);
        var bonus = await CountWrByAuthBonusAsync(ct);
        var stage = await CountWrByAuthStageAsync(ct);
        var allAuths = main.Keys.Concat(bonus.Keys).Concat(stage.Keys).Distinct();
        var totals = allAuths.ToDictionary(
            a => a,
            a => main.GetValueOrDefault(a) + bonus.GetValueOrDefault(a) + stage.GetValueOrDefault(a));
        return CountAhead(wrTotal, auth, totals);
    }

    private static int CountAhead(int value, int auth, Dictionary<int, int> counts) =>
        counts.Count(kv => kv.Value > value || (kv.Value == value && kv.Key < auth));

    private async Task<Dictionary<int, int>> CountWrByAuthMainAsync(CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(x => x.Time) });

        var holders = await (
            from pt in playerTimes
            join min in minTimesQuery on pt.Map equals min.Map
            where pt.Track == 0 && pt.Time == min.MinTime && pt.Auth != null
            select new { pt.Map, Auth = pt.Auth!.Value, pt.Id })
            .ToListAsync(ct);

        return holders
            .GroupBy(x => x.Map)
            .Select(g => g.OrderBy(x => x.Id).First().Auth)
            .GroupBy(auth => auth)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Dictionary<int, int>> CountWrByAuthBonusAsync(CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track > 0 && pt.Auth != null)
            .GroupBy(pt => new { pt.Map, pt.Track })
            .Select(g => new { g.Key.Map, g.Key.Track, MinTime = g.Min(x => x.Time) });

        var holders = await (
            from pt in playerTimes
            join min in minTimesQuery on new { pt.Map, pt.Track } equals new { min.Map, min.Track }
            where pt.Track > 0 && pt.Time == min.MinTime && pt.Auth != null
            select new { Group = $"{pt.Map}\0{pt.Track}", pt.Auth!.Value, pt.Id })
            .ToListAsync(ct);

        return holders
            .GroupBy(x => x.Group)
            .Select(g => g.OrderBy(x => x.Id).First().Value)
            .GroupBy(auth => auth)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Dictionary<int, int>> CountWrByAuthStageAsync(CancellationToken ct)
    {
        var minTimesQuery = stageTimes
            .GroupBy(st => new { st.Map, st.Track, st.Stage })
            .Select(g => new
            {
                g.Key.Map,
                g.Key.Track,
                g.Key.Stage,
                MinTime = g.Min(x => x.Time),
            });

        var holders = await (
            from st in stageTimes
            join min in minTimesQuery
                on new { st.Map, st.Track, st.Stage }
                equals new { min.Map, min.Track, min.Stage }
            where st.Time == min.MinTime
            select new { Group = $"{st.Map}\0{st.Track}\0{st.Stage}", st.Auth, st.Id })
            .ToListAsync(ct);

        return holders
            .GroupBy(x => x.Group)
            .Select(g => g.OrderBy(x => x.Id).First().Auth)
            .GroupBy(auth => auth)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
