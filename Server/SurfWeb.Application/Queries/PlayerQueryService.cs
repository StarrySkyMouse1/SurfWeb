using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Common;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Queries.Abstractions;

namespace SurfWeb.Application.Queries;

public sealed class PlayerQueryService(
    IUserReadRepository users,
    IPlayerReadRepository playerTimes,
    IMapReadRepository maps) : IPlayerQueryService
{
    public async Task<PlayerSummaryDto?> GetPlayerAsync(int auth, CancellationToken ct = default)
    {
        var user = await users.FindByAuthAsync(auth, ct);
        if (user is null) return null;

        var completions = await playerTimes.CountDistinctMapCompletionsAsync(auth, ct);
        var pointsRank = await users.CountAheadByPointsAsync(user.Points, auth, ct) + 1;
        var playtimeRank = await users.CountAheadByPlaytimeAsync(user.Playtime, auth, ct) + 1;
        var completionRank = await playerTimes.CountCompletionRankingsAheadAsync(completions, auth, ct) + 1;

        return new PlayerSummaryDto(
            user.Auth,
            user.Name,
            user.Points,
            user.Playtime,
            completions,
            pointsRank,
            playtimeRank,
            completionRank);
    }

    public async Task<(IReadOnlyList<PlayerTimeDto> Items, int Total)> GetPlayerTimesAsync(
        int auth, string? map, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var total = await playerTimes.CountPlayerTimesAsync(auth, map, ct);
        var rows = await playerTimes.ListPlayerTimesPageAsync(auth, map, skip, pageSize, ct);

        var items = rows.Select(pt => new PlayerTimeDto(
            pt.Id,
            pt.Map,
            pt.Style,
            pt.Track,
            pt.Time,
            TimeFormatter.Format(pt.Time),
            pt.Sync,
            TimeFormatter.FromUnixSeconds(pt.Date))).ToList();

        return (items, total);
    }

    public async Task<(IReadOnlyList<PlayerCompletionDto> Items, int Total)> GetPlayerCompletionsAsync(
        int auth, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await playerTimes.ListPlayerTimesForCompletionsAsync(auth, ct);
        var bestPerMap = rows
            .GroupBy(pt => pt.Map)
            .Select(g => g
                .OrderBy(pt => pt.Time)
                .ThenByDescending(pt => pt.Date)
                .ThenBy(pt => pt.Id)
                .First())
            .OrderByDescending(pt => pt.Date ?? 0)
            .ThenBy(pt => pt.Map)
            .ToList();

        var total = bestPerMap.Count;
        if (total == 0)
            return ([], 0);

        var skip = (page - 1) * pageSize;
        var pageRows = bestPerMap.Skip(skip).Take(pageSize).ToList();
        var mapNames = pageRows.Select(pt => pt.Map).ToList();

        var tiers = await maps.GetTiersByMapsAsync(mapNames, ct);

        var minTimes = await playerTimes.GetMinTimesByMapForCompletionsAsync(mapNames, ct);
        var wrByMap = minTimes
            .GroupBy(x => x.Map)
            .ToDictionary(g => g.Key, g => g.Min(x => x.MinTime));

        var items = pageRows
            .Select(row =>
            {
                tiers.TryGetValue(row.Map, out var tier);
                float? wrTime = wrByMap.TryGetValue(row.Map, out var wr) ? wr : null;
                float? gap = wrTime is not null ? row.Time - wrTime.Value : null;
                if (gap is <= 0.001f) gap = null;

                return new PlayerCompletionDto(
                    row.Map,
                    tier,
                    row.Time,
                    TimeFormatter.Format(row.Time),
                    row.Style,
                    row.Sync,
                    TimeFormatter.FromUnixSeconds(row.Date),
                    wrTime,
                    gap);
            })
            .ToList();

        return (items, total);
    }
}
