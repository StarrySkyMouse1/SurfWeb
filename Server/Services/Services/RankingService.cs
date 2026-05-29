using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Services.IServices;
using SurfWeb.Core.Constants;
using SurfWeb.Utils.Caching;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Core.Options;
using SurfWeb.Repositories;
using SurfWeb.Core.Models;

namespace SurfWeb.Services;

public sealed class RankingService(
    IBaseRepository<User> users,
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<StageTime> stageTimes,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IRankingService
{
    private sealed record CompletionRankEntry(int Auth, int Count);

    private sealed record WrCandidate(string GroupKey, int Auth, int Id);

    public async Task<(IReadOnlyList<RankingEntryDto> Items, int Total)> GetRankingsAsync(
        RankingType type,
        int page,
        int pageSize,
        WrRankingScope wrScope = WrRankingScope.Main,
        TrackRankingScope completionScope = TrackRankingScope.Main,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        if (skip >= SiteLimits.MaxRankingsTotal)
            return ([], SiteLimits.MaxRankingsTotal);

        var take = Math.Min(pageSize, SiteLimits.MaxRankingsTotal - skip);
        var ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.Cache.RankingsRefreshMinutes));
        var cacheKey = CacheKeys.Rankings(
            type,
            type == RankingType.Wr ? wrScope : null,
            type == RankingType.Completions ? completionScope : null);

        var snapshot = await cache.GetOrLoadAsync(
            cacheKey,
            ttl,
            token => LoadFullRankingsAsync(type, wrScope, completionScope, token),
            ct);

        var items = snapshot.Items.Skip(skip).Take(take).ToList();
        return (items, snapshot.Total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadFullRankingsAsync(
        RankingType type,
        WrRankingScope wrScope,
        TrackRankingScope completionScope,
        CancellationToken ct) =>
        type switch
        {
            RankingType.Playtime => await LoadPlaytimeSnapshotAsync(ct),
            RankingType.Completions => await LoadCompletionsSnapshotAsync(completionScope, ct),
            RankingType.Wr => wrScope switch
            {
                WrRankingScope.Bonus => await LoadBonusWrSnapshotAsync(ct),
                WrRankingScope.Stage => await LoadStageWrSnapshotAsync(ct),
                _ => await LoadMainWrSnapshotAsync(ct),
            },
            _ => await LoadPointsSnapshotAsync(ct),
        };

    private async Task<CachedPageList<RankingEntryDto>> LoadPointsSnapshotAsync(CancellationToken ct)
    {
        var rawTotal = await users.CountAsync(ct);
        var total = Math.Min(rawTotal, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var page = await users
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Auth)
            .Take(total)
            .ToListAsync(ct);
        var items = page
            .Select((u, i) => new RankingEntryDto(i + 1, u.Auth, u.Name, u.Points))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadPlaytimeSnapshotAsync(CancellationToken ct)
    {
        var rawTotal = await users.CountAsync(ct);
        var total = Math.Min(rawTotal, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var page = await users
            .OrderByDescending(u => u.Playtime)
            .ThenBy(u => u.Auth)
            .Take(total)
            .ToListAsync(ct);
        var items = page
            .Select((u, i) => new RankingEntryDto(i + 1, u.Auth, u.Name, u.Playtime))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadCompletionsSnapshotAsync(
        TrackRankingScope scope,
        CancellationToken ct)
    {
        var ranked = await ListCompletionRankingsAsync(scope, ct);
        var total = Math.Min(ranked.Count, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var top = ranked.Take(total).ToList();
        var authIds = top.Select(g => g.Auth).ToList();
        var names = await GetNamesByAuthIdsAsync(authIds, ct);
        var items = top
            .Select((g, i) => new RankingEntryDto(
                i + 1,
                g.Auth,
                names.GetValueOrDefault(g.Auth),
                g.Count))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadMainWrSnapshotAsync(CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(x => x.Time) });

        var wrCandidates = await (
            from pt in playerTimes
            join min in minTimesQuery on pt.Map equals min.Map
            where pt.Track == 0 && pt.Time == min.MinTime && pt.Auth != null
            select new WrCandidate(pt.Map, pt.Auth!.Value, pt.Id)
        ).ToListAsync(ct);

        return await BuildWrRankingSnapshotAsync(wrCandidates, ct);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadBonusWrSnapshotAsync(CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track > 0 && pt.Auth != null)
            .GroupBy(pt => new { pt.Map, pt.Track })
            .Select(g => new { g.Key.Map, g.Key.Track, MinTime = g.Min(x => x.Time) });

        var wrCandidates = await (
            from pt in playerTimes
            join min in minTimesQuery on new { pt.Map, pt.Track } equals new { min.Map, min.Track }
            where pt.Track > 0 && pt.Time == min.MinTime && pt.Auth != null
            select new WrCandidate($"{pt.Map}\0{pt.Track}", pt.Auth!.Value, pt.Id)
        ).ToListAsync(ct);

        return await BuildWrRankingSnapshotAsync(wrCandidates, ct);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadStageWrSnapshotAsync(CancellationToken ct)
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

        var wrCandidates = await (
            from st in stageTimes
            join min in minTimesQuery
                on new { st.Map, st.Track, st.Stage }
                equals new { min.Map, min.Track, min.Stage }
            where st.Time == min.MinTime
            select new WrCandidate($"{st.Map}\0{st.Track}\0{st.Stage}", st.Auth, st.Id)
        ).ToListAsync(ct);

        return await BuildWrRankingSnapshotAsync(wrCandidates, ct);
    }

    private async Task<CachedPageList<RankingEntryDto>> BuildWrRankingSnapshotAsync(
        IReadOnlyList<WrCandidate> wrCandidates,
        CancellationToken ct)
    {
        var wrCounts = wrCandidates
            .GroupBy(x => x.GroupKey)
            .Select(g => g.OrderBy(x => x.Id).First().Auth)
            .GroupBy(auth => auth)
            .Select(g => new { Auth = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Auth)
            .ToList();

        var total = Math.Min(wrCounts.Count, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var top = wrCounts.Take(total).ToList();
        var names = await GetNamesByAuthIdsAsync(top.Select(x => x.Auth).ToList(), ct);
        var items = top
            .Select((x, i) => new RankingEntryDto(
                i + 1,
                x.Auth,
                names.GetValueOrDefault(x.Auth),
                x.Count))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }

    private async Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(
        TrackRankingScope scope,
        CancellationToken ct)
    {
        if (scope == TrackRankingScope.Bonus)
        {
            // EF 无法翻译嵌套 GroupBy / GroupBy 内 Distinct；库内去重 (玩家,地图,赛道) 后内存按玩家计数
            var authPerMapTrack = await playerTimes
                .Where(pt => pt.Auth != null && pt.Track > 0)
                .GroupBy(pt => new { Auth = pt.Auth!.Value, pt.Map, pt.Track })
                .Select(g => g.Key.Auth)
                .ToListAsync(ct);

            return authPerMapTrack
                .GroupBy(auth => auth)
                .Select(g => new CompletionRankEntry(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Auth)
                .ToList();
        }

        var ranked = await playerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, Count = g.Select(x => x.Map).Distinct().Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Auth)
            .ToListAsync(ct);
        return ranked.Select(x => new CompletionRankEntry(x.Auth, x.Count)).ToList();
    }

    private async Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(
        IReadOnlyList<int> authIds, CancellationToken ct)
    {
        if (authIds.Count == 0) return new Dictionary<int, string?>();
        return await users
            .Where(u => authIds.Contains(u.Auth))
            .ToDictionaryAsync(u => u.Auth, u => u.Name, ct);
    }
}
