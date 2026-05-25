using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Services.IServices;
using SurfWeb.Data.Caching;
using SurfWeb.Utils.Caching;
using SurfWeb.Utils.Constants;
using SurfWeb.Data.Dtos;
using SurfWeb.Configurations;
using SurfWeb.Repositories;
using SurfWeb.Repositories.Entities;

namespace SurfWeb.Services;

public sealed class RankingService(
    IBaseRepository<User> users,
    IBaseRepository<PlayerTime> playerTimes,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IRankingService
{
    private sealed record CompletionRankEntry(int Auth, int Count);

    public async Task<(IReadOnlyList<RankingEntryDto> Items, int Total)> GetRankingsAsync(
        string type, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        if (skip >= SiteLimits.MaxRankingsTotal)
            return ([], SiteLimits.MaxRankingsTotal);

        var take = Math.Min(pageSize, SiteLimits.MaxRankingsTotal - skip);
        var typeKey = type.ToLowerInvariant();
        var ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.Cache.RankingsRefreshMinutes));

        var snapshot = await cache.GetOrLoadAsync(
            CacheKeys.Rankings(typeKey),
            ttl,
            token => LoadFullRankingsAsync(typeKey, token),
            ct);

        var items = snapshot.Items.Skip(skip).Take(take).ToList();
        return (items, snapshot.Total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadFullRankingsAsync(
        string typeKey,
        CancellationToken ct) =>
        typeKey switch
        {
            "playtime" => await LoadPlaytimeSnapshotAsync(ct),
            "completions" => await LoadCompletionsSnapshotAsync(ct),
            "wr" => await LoadWrSnapshotAsync(ct),
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

    private async Task<CachedPageList<RankingEntryDto>> LoadCompletionsSnapshotAsync(CancellationToken ct)
    {
        var ranked = await ListCompletionRankingsAsync(ct);
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

    private async Task<CachedPageList<RankingEntryDto>> LoadWrSnapshotAsync(CancellationToken ct)
    {
        var minTimesQuery = playerTimes
            .Where(pt => pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(x => x.Time) });

        var wrCandidates = await (
            from pt in playerTimes
            join min in minTimesQuery on pt.Map equals min.Map
            where pt.Track == 0 && pt.Time == min.MinTime && pt.Auth != null
            select new { pt.Map, Auth = pt.Auth!.Value, pt.Id }
        ).ToListAsync(ct);

        var wrCounts = wrCandidates
            .GroupBy(x => x.Map)
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

    private async Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(CancellationToken ct)
    {
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
