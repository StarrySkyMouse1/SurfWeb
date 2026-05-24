using Microsoft.Extensions.Options;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Constants;

namespace SurfWeb.Application.Queries;

public sealed class RankingQueryService(
    IUserReadRepository users,
    IPlayerReadRepository playerTimes,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IRankingQueryService
{
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
            _ => await LoadPointsSnapshotAsync(ct),
        };

    private async Task<CachedPageList<RankingEntryDto>> LoadPointsSnapshotAsync(CancellationToken ct)
    {
        var rawTotal = await users.CountAllAsync(ct);
        var total = Math.Min(rawTotal, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var page = await users.ListOrderedByPointsAsync(0, total, ct);
        var items = page
            .Select((u, i) => new RankingEntryDto(i + 1, u.Auth, u.Name, u.Points))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadPlaytimeSnapshotAsync(CancellationToken ct)
    {
        var rawTotal = await users.CountAllAsync(ct);
        var total = Math.Min(rawTotal, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var page = await users.ListOrderedByPlaytimeAsync(0, total, ct);
        var items = page
            .Select((u, i) => new RankingEntryDto(i + 1, u.Auth, u.Name, u.Playtime))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }

    private async Task<CachedPageList<RankingEntryDto>> LoadCompletionsSnapshotAsync(CancellationToken ct)
    {
        var ranked = await playerTimes.ListCompletionRankingsAsync(ct);
        var total = Math.Min(ranked.Count, SiteLimits.MaxRankingsTotal);
        if (total == 0)
            return new CachedPageList<RankingEntryDto>([], 0);

        var top = ranked.Take(total).ToList();
        var authIds = top.Select(g => g.Auth).ToList();
        var names = await users.GetNamesByAuthIdsAsync(authIds, ct);
        var items = top
            .Select((g, i) => new RankingEntryDto(
                i + 1,
                g.Auth,
                names.GetValueOrDefault(g.Auth),
                g.Count))
            .ToList();
        return new CachedPageList<RankingEntryDto>(items, total);
    }
}
