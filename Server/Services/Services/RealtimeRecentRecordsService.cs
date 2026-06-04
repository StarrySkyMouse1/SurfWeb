using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Constants;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Core.Models;
using SurfWeb.Core.Options;
using SurfWeb.Repositories;
using SurfWeb.Services.IServices;
using SurfWeb.Utils.Common;

namespace SurfWeb.Services;

public sealed class RealtimeRecentRecordsService(
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<StageTime> stageTimes,
    IBaseRepository<User> users,
    IBaseRepository<MapTier> mapTiers,
    IOptions<SurfWebOptions> options) : IRealtimeRecentRecordsService
{
    private const float GapEpsilon = 0.001f;
    private readonly byte _defaultStyle = options.Value.DefaultStyleId;

    public async Task<(int PlayerTimeId, int StageTimeId)> GetHighWaterMarksAsync(CancellationToken ct = default)
    {
        var maxPt = await playerTimes.MaxAsync(pt => (int?)pt.Id, ct) ?? 0;
        var maxSt = await stageTimes.MaxAsync(st => (int?)st.Id, ct) ?? 0;
        return (maxPt, maxSt);
    }

    public async Task<RealtimeRecentRecordsPollResult> PollNewSinceAsync(
        int afterPlayerTimeId,
        int afterStageTimeId,
        CancellationToken ct = default)
    {
        var newPlayerRuns = await playerTimes
            .Where(pt => pt.Id > afterPlayerTimeId && pt.Auth != null && pt.Date != null)
            .OrderBy(pt => pt.Id)
            .Take(500)
            .ToListAsync(ct);

        var newStageRuns = await stageTimes
            .Where(st => st.Id > afterStageTimeId && st.Date != null)
            .OrderBy(st => st.Id)
            .Take(500)
            .ToListAsync(ct);

        var lastPt = newPlayerRuns.Count > 0 ? newPlayerRuns[^1].Id : afterPlayerTimeId;
        var lastSt = newStageRuns.Count > 0 ? newStageRuns[^1].Id : afterStageTimeId;

        if (newPlayerRuns.Count == 0 && newStageRuns.Count == 0)
            return new RealtimeRecentRecordsPollResult([], lastPt, lastSt);

        var items = await BuildDtosAsync(newPlayerRuns, newStageRuns, ct);
        return new RealtimeRecentRecordsPollResult(items, lastPt, lastSt);
    }

    public async Task<(IReadOnlyList<RealtimeRecentRecordDto> Items, int Total)> GetRecentPageAsync(
        RealtimeRecentRecordScope scope,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var maxTotal = SiteLimits.MaxRecentTotal;

        var merged = await LoadScopedRecentRunsAsync(scope, ct);
        var total = Math.Min(merged.Count, maxTotal);
        if (total == 0)
            return ([], 0);

        var pageRuns = merged
            .Skip((page - 1) * pageSize)
            .Take(Math.Min(pageSize, total - (page - 1) * pageSize))
            .ToList();

        if (pageRuns.Count == 0)
            return ([], total);

        var playerRuns = pageRuns.Where(r => r.StageRun is null).Select(r => r.PlayerRun!).ToList();
        var stageRuns = pageRuns.Where(r => r.StageRun is not null).Select(r => r.StageRun!).ToList();
        var items = await BuildDtosAsync(playerRuns, stageRuns, ct);
        return (items, total);
    }

    private async Task<List<ScopedRun>> LoadScopedRecentRunsAsync(
        RealtimeRecentRecordScope scope,
        CancellationToken ct)
    {
        return scope switch
        {
            RealtimeRecentRecordScope.Stage => await LoadStageScopedAsync(ct),
            RealtimeRecentRecordScope.Main => await LoadPlayerScopedAsync(RealtimeRecentRecordScope.Main, ct),
            RealtimeRecentRecordScope.Bonus => await LoadPlayerScopedAsync(RealtimeRecentRecordScope.Bonus, ct),
            _ => await LoadAllScopedAsync(ct),
        };
    }

    private async Task<List<ScopedRun>> LoadAllScopedAsync(CancellationToken ct)
    {
        var recentPlayerRuns = await playerTimes
            .Where(pt => pt.Auth != null && pt.Date != null && pt.Style == _defaultStyle)
            .OrderByDescending(pt => pt.Date)
            .ThenByDescending(pt => pt.Id)
            .Take(SiteLimits.RecentScanBatch)
            .ToListAsync(ct);

        var recentStageRuns = await stageTimes
            .Where(st => st.Date != null && st.Style == _defaultStyle)
            .OrderByDescending(st => st.Date)
            .ThenByDescending(st => st.Id)
            .Take(SiteLimits.RecentScanBatch)
            .ToListAsync(ct);

        return PickTopPlayerPbByDate(recentPlayerRuns)
            .Select(pt => new ScopedRun(pt, null, pt.Date ?? 0, pt.Id))
            .Concat(PickTopStagePbByDate(recentStageRuns)
                .Select(st => new ScopedRun(null, st, st.Date ?? 0, st.Id)))
            .OrderByDescending(r => r.DateUnix)
            .ThenByDescending(r => r.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();
    }

    private async Task<List<ScopedRun>> LoadPlayerScopedAsync(
        RealtimeRecentRecordScope scope,
        CancellationToken ct)
    {
        var query = playerTimes.Where(pt => pt.Auth != null && pt.Date != null && pt.Style == _defaultStyle);
        query = scope switch
        {
            RealtimeRecentRecordScope.Main => query.Where(pt => pt.Track == 0),
            RealtimeRecentRecordScope.Bonus => query.Where(pt => pt.Track > 0),
            _ => query,
        };

        var recentRuns = await query
            .OrderByDescending(pt => pt.Date)
            .ThenByDescending(pt => pt.Id)
            .Take(SiteLimits.RecentScanBatch)
            .ToListAsync(ct);

        return PickTopPlayerPbByDate(recentRuns)
            .Select(pt => new ScopedRun(pt, null, pt.Date ?? 0, pt.Id))
            .ToList();
    }

    private async Task<List<ScopedRun>> LoadStageScopedAsync(CancellationToken ct)
    {
        var recentRuns = await stageTimes
            .Where(st => st.Date != null && st.Style == _defaultStyle)
            .OrderByDescending(st => st.Date)
            .ThenByDescending(st => st.Id)
            .Take(SiteLimits.RecentScanBatch)
            .ToListAsync(ct);

        return PickTopStagePbByDate(recentRuns)
            .Select(st => new ScopedRun(null, st, st.Date ?? 0, st.Id))
            .ToList();
    }

    private async Task<IReadOnlyList<RealtimeRecentRecordDto>> BuildDtosAsync(
        IReadOnlyList<PlayerTime> playerRuns,
        IReadOnlyList<StageTime> stageRuns,
        CancellationToken ct)
    {
        if (playerRuns.Count == 0 && stageRuns.Count == 0)
            return [];

        var maps = playerRuns.Select(pt => pt.Map)
            .Concat(stageRuns.Select(st => st.Map))
            .Distinct()
            .ToList();

        var runHistoricalTimes = await GetHistoricalRunTimesAsync(playerRuns, ct);
        var stageHistoricalTimes = await GetHistoricalStageTimesAsync(stageRuns, ct);

        var authIds = playerRuns
            .Where(pt => pt.Auth != null)
            .Select(pt => pt.Auth!.Value)
            .Concat(stageRuns.Select(st => st.Auth))
            .Distinct()
            .ToList();
        var names = await GetNamesByAuthIdsAsync(authIds, ct);
        var tiers = await GetTiersByMapsAsync(maps, ct);

        var dtos = new List<RealtimeRecentRecordDto>(playerRuns.Count + stageRuns.Count);
        foreach (var pt in playerRuns)
            dtos.Add(ToDto(pt, names, tiers, runHistoricalTimes));
        foreach (var st in stageRuns)
            dtos.Add(ToDto(st, names, tiers, stageHistoricalTimes));

        return dtos
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.Id)
            .ToList();
    }

    private RealtimeRecentRecordDto ToDto(
        PlayerTime pt,
        Dictionary<int, string?> names,
        Dictionary<string, int> tiers,
        Dictionary<int, HistoricalTimes> historicalTimes)
    {
        names.TryGetValue(pt.Auth!.Value, out var name);
        var historical = historicalTimes.GetValueOrDefault(pt.Id);
        float? firstTime = historical?.FirstPlaceTime;
        float? pbTime = historical?.PersonalBestTime;
        tiers.TryGetValue(pt.Map, out var tier);

        var (gapFirst, gapPb) = ComputeGaps(pt.Time, firstTime, pbTime);

        return new RealtimeRecentRecordDto(
            pt.Id,
            pt.Auth!.Value,
            name,
            pt.Map,
            pt.Style,
            pt.Track,
            null,
            pt.Time,
            TimeFormatter.Format(pt.Time),
            TimeFormatter.FromUnixSeconds(pt.Date),
            tier,
            firstTime,
            firstTime is not null ? TimeFormatter.Format(firstTime.Value) : null,
            gapFirst,
            pbTime,
            pbTime is not null ? TimeFormatter.Format(pbTime.Value) : null,
            gapPb);
    }

    private RealtimeRecentRecordDto ToDto(
        StageTime st,
        Dictionary<int, string?> names,
        Dictionary<string, int> tiers,
        Dictionary<int, HistoricalTimes> historicalTimes)
    {
        names.TryGetValue(st.Auth, out var name);
        var historical = historicalTimes.GetValueOrDefault(st.Id);
        float? firstTime = historical?.FirstPlaceTime;
        float? pbTime = historical?.PersonalBestTime;
        tiers.TryGetValue(st.Map, out var tier);

        var (gapFirst, gapPb) = ComputeGaps(st.Time, firstTime, pbTime);

        return new RealtimeRecentRecordDto(
            st.Id,
            st.Auth,
            name,
            st.Map,
            st.Style,
            st.Track,
            st.Stage,
            st.Time,
            TimeFormatter.Format(st.Time),
            TimeFormatter.FromUnixSeconds(st.Date),
            tier,
            firstTime,
            firstTime is not null ? TimeFormatter.Format(firstTime.Value) : null,
            gapFirst,
            pbTime,
            pbTime is not null ? TimeFormatter.Format(pbTime.Value) : null,
            gapPb);
    }

    private static (float? GapFromFirst, float? GapFromPersonalBest) ComputeGaps(
        float time,
        float? firstTime,
        float? personalBestTime)
    {
        float? gapFirst = firstTime is not null ? time - firstTime.Value : null;
        if (gapFirst is not null && Math.Abs(gapFirst.Value) <= GapEpsilon)
            gapFirst = firstTime is not null ? 0 : null;

        float? gapPb = personalBestTime is not null ? time - personalBestTime.Value : null;
        if (gapPb is not null && Math.Abs(gapPb.Value) <= GapEpsilon)
            gapPb = personalBestTime is not null ? 0 : null;

        return (gapFirst, gapPb);
    }

    private async Task<Dictionary<int, HistoricalTimes>> GetHistoricalRunTimesAsync(
        IReadOnlyList<PlayerTime> runs,
        CancellationToken ct)
    {
        if (runs.Count == 0)
            return new Dictionary<int, HistoricalTimes>();

        var authIds = runs.Where(pt => pt.Auth != null).Select(pt => pt.Auth!.Value).Distinct().ToList();
        var maps = runs.Select(pt => pt.Map).Distinct().ToList();
        var tracks = runs.Select(pt => pt.Track).Distinct().ToList();

        var candidates = await playerTimes
            .Where(pt =>
                pt.Auth != null
                && authIds.Contains(pt.Auth.Value)
                && pt.Date != null
                && maps.Contains(pt.Map)
                && tracks.Contains(pt.Track)
                && pt.Style == _defaultStyle)
            .ToListAsync(ct);

        return runs.ToDictionary(
            pt => pt.Id,
            pt =>
            {
                var history = candidates
                    .Where(candidate => candidate.Map == pt.Map
                        && candidate.Track == pt.Track
                        && IsAtOrBefore(candidate.Date, candidate.Id, pt.Date, pt.Id))
                    .ToList();

                var firstTime = history.Count > 0
                    ? history.Min(candidate => candidate.Time)
                    : pt.Time;

                float? pbTime = null;
                if (pt.Auth is not null)
                {
                    var personalTimes = candidates
                        .Where(candidate => candidate.Map == pt.Map
                            && candidate.Track == pt.Track
                            && candidate.Auth!.Value == pt.Auth.Value
                            && IsAtOrBefore(candidate.Date, candidate.Id, pt.Date, pt.Id))
                        .Select(candidate => candidate.Time)
                        .ToList();
                    if (personalTimes.Count > 0)
                        pbTime = personalTimes.Min();
                }

                return new HistoricalTimes(firstTime, pbTime);
            });
    }

    private async Task<Dictionary<int, HistoricalTimes>> GetHistoricalStageTimesAsync(
        IReadOnlyList<StageTime> runs,
        CancellationToken ct)
    {
        if (runs.Count == 0)
            return new Dictionary<int, HistoricalTimes>();

        var authIds = runs.Select(st => st.Auth).Distinct().ToList();
        var maps = runs.Select(st => st.Map).Distinct().ToList();
        var tracks = runs.Select(st => st.Track).Distinct().ToList();
        var stages = runs.Select(st => st.Stage).Distinct().ToList();

        var candidates = await stageTimes
            .Where(st =>
                authIds.Contains(st.Auth)
                && st.Date != null
                && maps.Contains(st.Map)
                && tracks.Contains(st.Track)
                && stages.Contains(st.Stage)
                && st.Style == _defaultStyle)
            .ToListAsync(ct);

        return runs.ToDictionary(
            st => st.Id,
            st =>
            {
                var history = candidates
                    .Where(candidate => candidate.Map == st.Map
                        && candidate.Track == st.Track
                        && candidate.Stage == st.Stage
                        && IsAtOrBefore(candidate.Date, candidate.Id, st.Date, st.Id))
                    .ToList();

                var firstTime = history.Count > 0
                    ? history.Min(candidate => candidate.Time)
                    : st.Time;

                var personalTimes = candidates
                    .Where(candidate => candidate.Map == st.Map
                        && candidate.Track == st.Track
                        && candidate.Stage == st.Stage
                        && candidate.Auth == st.Auth
                        && IsAtOrBefore(candidate.Date, candidate.Id, st.Date, st.Id))
                    .Select(candidate => candidate.Time)
                    .ToList();
                float? pbTime = personalTimes.Count > 0 ? personalTimes.Min() : null;

                return new HistoricalTimes(firstTime, pbTime);
            });
    }

    private static bool IsAtOrBefore(int? candidateDate, int candidateId, int? recordDate, int recordId)
    {
        if (recordDate is null)
            return candidateId <= recordId;

        if (candidateDate is null)
            return false;

        return candidateDate < recordDate
            || (candidateDate == recordDate && candidateId <= recordId);
    }

    private async Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(
        IReadOnlyList<int> authIds,
        CancellationToken ct)
    {
        if (authIds.Count == 0)
            return new Dictionary<int, string?>();

        return await users
            .Where(u => authIds.Contains(u.Auth))
            .ToDictionaryAsync(u => u.Auth, u => u.Name, ct);
    }

    private async Task<Dictionary<string, int>> GetTiersByMapsAsync(
        IReadOnlyList<string> mapNames,
        CancellationToken ct)
    {
        if (mapNames.Count == 0)
            return new Dictionary<string, int>();

        return await mapTiers
            .Where(mt => mapNames.Contains(mt.Map))
            .ToDictionaryAsync(mt => mt.Map, mt => (int)mt.Tier, ct);
    }

    private static List<PlayerTime> PickTopPlayerPbByDate(IEnumerable<PlayerTime> runs) =>
        DedupeFastestPerPlayerMapTrack(runs)
            .OrderByDescending(pt => pt.Date)
            .ThenByDescending(pt => pt.Id)
            .Take(SiteLimits.MaxRecentTotal)
            .ToList();

    private static List<StageTime> PickTopStagePbByDate(IEnumerable<StageTime> runs) =>
        DedupeFastestPerStage(runs)
            .OrderByDescending(st => st.Date)
            .ThenByDescending(st => st.Id)
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

    private sealed record HistoricalTimes(float FirstPlaceTime, float? PersonalBestTime);

    private sealed record ScopedRun(PlayerTime? PlayerRun, StageTime? StageRun, int DateUnix, int Id);
}
