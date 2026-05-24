using Microsoft.Extensions.Options;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Common;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Constants;
using SurfWeb.Domain.Entities;

namespace SurfWeb.Application.Queries;

public sealed class RecordQueryService(
    IPlayerReadRepository playerTimes,
    IUserReadRepository users,
    IQueryCache cache,
    IOptions<SurfWebOptions> options) : IRecordQueryService
{
    public async Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
        int page, int pageSize, CancellationToken ct = default)
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

        if (snapshot.Total == 0)
            return ([], 0);

        var take = Math.Min(pageSize, snapshot.Total - (page - 1) * pageSize);
        var items = snapshot.Items
            .Skip((page - 1) * pageSize)
            .Take(take)
            .ToList();
        return (items, snapshot.Total);
    }

    private async Task<CachedPageList<RecentRecordDto>> LoadRecentSnapshotAsync(CancellationToken ct)
    {
        var recent = await playerTimes.ScanRecentPlayerTimesAsync(SiteLimits.RecentScanBatch, ct);

        var deduped = recent
            .GroupBy(pt => (Auth: pt.Auth!.Value, pt.Map))
            .Select(g => g
                .OrderByDescending(pt => pt.Date)
                .ThenBy(pt => pt.Track)
                .ThenBy(pt => pt.Id)
                .First())
            .OrderByDescending(pt => pt.Date)
            .ToList();

        var limited = deduped.Take(SiteLimits.MaxRecentTotal).ToList();
        var total = limited.Count;
        if (total == 0)
            return new CachedPageList<RecentRecordDto>([], 0);

        var mapList = limited.Select(pt => pt.Map).Distinct().ToList();
        var minTimes = await playerTimes.GetMinTimesByMapTrackAsync(mapList, ct);
        var wrByMap = minTimes
            .GroupBy(x => (x.Map, x.Track))
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));

        var authIds = limited.Select(pt => pt.Auth!.Value).Distinct().ToList();
        var names = await users.GetNamesByAuthIdsAsync(authIds, ct);

        var items = limited
            .Select(pt => ToRecentRecordDto(pt, names, wrByMap))
            .ToList();

        return new CachedPageList<RecentRecordDto>(items, total);
    }

    private static RecentRecordDto ToRecentRecordDto(
        PlayerTime pt,
        Dictionary<int, string?> names,
        Dictionary<(string Map, byte Track), float> wrByMap)
    {
        names.TryGetValue(pt.Auth!.Value, out var name);
        var key = (pt.Map, pt.Track);
        float? wrTime = wrByMap.TryGetValue(key, out var wr) ? wr : null;
        float? gap = wrTime is not null ? pt.Time - wrTime.Value : null;
        if (gap is <= 0.001f) gap = null;

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
            gap);
    }
}
