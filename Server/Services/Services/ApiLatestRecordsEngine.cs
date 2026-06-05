using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Constants;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Core.Models;
using SurfWeb.Core.Options;
using SurfWeb.Repositories;
using SurfWeb.Utils.Common;

namespace SurfWeb.Services;

/// <summary>对外最新记录查询（REST 主实现；后续 SignalR 可复用）。</summary>
public sealed class ApiLatestRecordsEngine(
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<StageTime> stageTimes,
    IBaseRepository<User> users,
    IBaseRepository<MapTier> mapTiers,
    IOptions<SurfWebOptions> options)
{
    private const float GapEpsilon = 0.001f;
    private readonly byte _defaultStyle = options.Value.DefaultStyleId;

    public async Task<IReadOnlyList<ApiLatestRecordDto>> QueryAsync(
        RealtimeRecentRecordScope scope,
        DateTimeOffset? after,
        CancellationToken ct)
    {
        var limit = after is null
            ? SiteLimits.ApiLatestRecordsInitialCount
            : SiteLimits.ApiLatestRecordsCount;
        var afterUnix = after?.ToUnixTimeSeconds();

        var merged = await LoadMergedRecentAsync(scope, ct);
        if (merged.Count == 0)
            return [];

        var page = afterUnix is null
            ? merged
                .OrderByDescending(r => r.DateUnix)
                .ThenByDescending(r => r.Id)
                .Take(limit)
                .ToList()
            : merged
                .Where(r => r.DateUnix > afterUnix)
                .OrderBy(r => r.DateUnix)
                .ThenBy(r => r.Id)
                .Take(limit)
                .ToList();

        if (page.Count == 0)
            return [];

        var playerRuns = page.Where(r => r.PlayerRun is not null).Select(r => r.PlayerRun!).ToList();
        var stageRuns = page.Where(r => r.StageRun is not null).Select(r => r.StageRun!).ToList();
        return await BuildDtosAsync(playerRuns, stageRuns, newestFirst: afterUnix is null, ct);
    }

    private async Task<List<MergedRun>> LoadMergedRecentAsync(
        RealtimeRecentRecordScope scope,
        CancellationToken ct)
    {
        var runs = new List<MergedRun>();

        if (scope != RealtimeRecentRecordScope.Stage)
        {
            var query = playerTimes
                .Where(pt => pt.Auth != null && pt.Date != null && pt.Style == _defaultStyle);
            query = scope switch
            {
                RealtimeRecentRecordScope.Main => query.Where(pt => pt.Track == 0),
                RealtimeRecentRecordScope.Bonus => query.Where(pt => pt.Track > 0),
                _ => query,
            };

            var batch = await query
                .OrderByDescending(pt => pt.Date)
                .ThenByDescending(pt => pt.Id)
                .Take(SiteLimits.RecentScanBatch)
                .ToListAsync(ct);

            runs.AddRange(batch.Select(pt => new MergedRun(pt, null, pt.Date ?? 0, pt.Id)));
        }

        if (scope is RealtimeRecentRecordScope.Stage or RealtimeRecentRecordScope.All)
        {
            var batch = await stageTimes
                .Where(st => st.Date != null && st.Style == _defaultStyle)
                .OrderByDescending(st => st.Date)
                .ThenByDescending(st => st.Id)
                .Take(SiteLimits.RecentScanBatch)
                .ToListAsync(ct);

            runs.AddRange(batch.Select(st => new MergedRun(null, st, st.Date ?? 0, st.Id)));
        }

        return runs;
    }

    private async Task<IReadOnlyList<ApiLatestRecordDto>> BuildDtosAsync(
        IReadOnlyList<PlayerTime> playerRuns,
        IReadOnlyList<StageTime> stageRuns,
        bool newestFirst,
        CancellationToken ct)
    {
        var maps = playerRuns.Select(pt => pt.Map)
            .Concat(stageRuns.Select(st => st.Map))
            .Distinct()
            .ToList();

        var runWrAtMoment = await GetHistoricalRunWrAsync(playerRuns, ct);
        var stageWrAtMoment = await GetHistoricalStageWrAsync(stageRuns, ct);

        var authIds = playerRuns
            .Where(pt => pt.Auth != null)
            .Select(pt => pt.Auth!.Value)
            .Concat(stageRuns.Select(st => st.Auth))
            .Distinct()
            .ToList();
        var names = await GetNamesByAuthIdsAsync(authIds, ct);
        var tiers = await GetTiersByMapsAsync(maps, ct);

        var dtos = new List<ApiLatestRecordDto>(playerRuns.Count + stageRuns.Count);
        foreach (var pt in playerRuns)
            dtos.Add(ToDto(pt, names, tiers, runWrAtMoment));
        foreach (var st in stageRuns)
            dtos.Add(ToDto(st, names, tiers, stageWrAtMoment));

        return newestFirst
            ? dtos.OrderByDescending(d => d.RecordedAt).ThenByDescending(d => d.Type).ToList()
            : dtos.OrderBy(d => d.RecordedAt).ThenBy(d => d.Type).ToList();
    }

    private static ApiLatestRecordDto ToDto(
        PlayerTime pt,
        Dictionary<int, string?> names,
        Dictionary<string, int> tiers,
        Dictionary<int, float> wrAtMoment)
    {
        names.TryGetValue(pt.Auth!.Value, out var name);
        wrAtMoment.TryGetValue(pt.Id, out var wrTime);
        tiers.TryGetValue(pt.Map, out var tier);

        return new ApiLatestRecordDto(
            name,
            pt.Map,
            tier,
            pt.Track == 0 ? "main" : "bonus",
            pt.Track,
            null,
            pt.Track == 0 ? "主线" : $"奖励 {pt.Track}",
            TimeFormatter.FromUnixSeconds(pt.Date),
            FormatGap(ComputeGapFromWr(pt.Time, wrTime)));
    }

    private static ApiLatestRecordDto ToDto(
        StageTime st,
        Dictionary<int, string?> names,
        Dictionary<string, int> tiers,
        Dictionary<int, float> wrAtMoment)
    {
        names.TryGetValue(st.Auth, out var name);
        wrAtMoment.TryGetValue(st.Id, out var wrTime);
        tiers.TryGetValue(st.Map, out var tier);

        return new ApiLatestRecordDto(
            name,
            st.Map,
            tier,
            "stage",
            st.Track,
            st.Stage,
            $"阶段 {st.Stage}",
            TimeFormatter.FromUnixSeconds(st.Date),
            FormatGap(ComputeGapFromWr(st.Time, wrTime)));
    }

    private static float? ComputeGapFromWr(float time, float wrTime)
    {
        float? gap = time - wrTime;
        if (Math.Abs(gap.Value) <= GapEpsilon)
            return 0;

        return gap;
    }

    private static string FormatGap(float? gapSeconds)
    {
        if (gapSeconds is null || Math.Abs(gapSeconds.Value) <= GapEpsilon)
            return "+0.000";

        return gapSeconds.Value > 0
            ? $"+{gapSeconds.Value:F3}"
            : $"{gapSeconds.Value:F3}";
    }

    private async Task<Dictionary<int, float>> GetHistoricalRunWrAsync(
        IReadOnlyList<PlayerTime> runs,
        CancellationToken ct)
    {
        if (runs.Count == 0)
            return new Dictionary<int, float>();

        var maps = runs.Select(pt => pt.Map).Distinct().ToList();
        var tracks = runs.Select(pt => pt.Track).Distinct().ToList();

        var candidates = await playerTimes
            .Where(pt =>
                pt.Date != null
                && maps.Contains(pt.Map)
                && tracks.Contains(pt.Track)
                && pt.Style == _defaultStyle)
            .ToListAsync(ct);

        return runs.ToDictionary(
            pt => pt.Id,
            pt =>
            {
                var history = candidates
                    .Where(c => c.Map == pt.Map
                        && c.Track == pt.Track
                        && IsAtOrBefore(c.Date, c.Id, pt.Date, pt.Id))
                    .ToList();

                return history.Count > 0
                    ? history.Min(c => c.Time)
                    : pt.Time;
            });
    }

    private async Task<Dictionary<int, float>> GetHistoricalStageWrAsync(
        IReadOnlyList<StageTime> runs,
        CancellationToken ct)
    {
        if (runs.Count == 0)
            return new Dictionary<int, float>();

        var maps = runs.Select(st => st.Map).Distinct().ToList();
        var tracks = runs.Select(st => st.Track).Distinct().ToList();
        var stages = runs.Select(st => st.Stage).Distinct().ToList();

        var candidates = await stageTimes
            .Where(st =>
                st.Date != null
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
                    .Where(c => c.Map == st.Map
                        && c.Track == st.Track
                        && c.Stage == st.Stage
                        && IsAtOrBefore(c.Date, c.Id, st.Date, st.Id))
                    .ToList();

                return history.Count > 0
                    ? history.Min(c => c.Time)
                    : st.Time;
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

    private sealed record MergedRun(PlayerTime? PlayerRun, StageTime? StageRun, int DateUnix, int Id);
}
