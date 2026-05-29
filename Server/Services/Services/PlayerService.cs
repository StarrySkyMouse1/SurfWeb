using Microsoft.EntityFrameworkCore;
using SurfWeb.Services.IServices;
using SurfWeb.Utils.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Repositories;
using SurfWeb.Core.Models;

namespace SurfWeb.Services;

public sealed class PlayerService(
    IBaseRepository<User> users,
    IBaseRepository<PlayerTime> playerTimes,
    IBaseRepository<MapTier> mapTiers) : IPlayerService
{
    public async Task<PlayerSummaryDto?> GetPlayerAsync(int auth, CancellationToken ct = default)
    {
        var user = await users.FirstOrDefaultAsync(u => u.Auth == auth, ct);
        if (user is null) return null;

        var completions = await CountDistinctMapCompletionsAsync(auth, ct);
        var pointsRank = await CountAheadByPointsAsync(user.Points, auth, ct) + 1;
        var playtimeRank = await CountAheadByPlaytimeAsync(user.Playtime, auth, ct) + 1;
        var completionRank = await CountCompletionRankingsAheadAsync(completions, auth, ct) + 1;

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

        var total = await CountPlayerTimesAsync(auth, map, ct);
        var rows = await ListPlayerTimesPageAsync(auth, map, skip, pageSize, ct);

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

        var rows = await ListPlayerTimesForCompletionsAsync(auth, ct);
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

        var tiers = await GetTiersByMapsAsync(mapNames, ct);

        var minTimes = await GetMinTimesByMapForCompletionsAsync(mapNames, ct);
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

    private Task<int> CountDistinctMapCompletionsAsync(int auth, CancellationToken ct) =>
        playerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .Select(pt => pt.Map)
            .Distinct()
            .CountAsync(ct);

    private Task<int> CountPlayerTimesAsync(int auth, string? map, CancellationToken ct)
    {
        var query = playerTimes.Where(pt => pt.Auth == auth);
        if (!string.IsNullOrWhiteSpace(map))
            query = query.Where(pt => pt.Map == map);
        return query.CountAsync(ct);
    }

    private async Task<IReadOnlyList<PlayerTime>> ListPlayerTimesPageAsync(
        int auth, string? map, int skip, int take, CancellationToken ct)
    {
        var query = playerTimes.Where(pt => pt.Auth == auth);
        if (!string.IsNullOrWhiteSpace(map))
            query = query.Where(pt => pt.Map == map);
        return await query
            .OrderByDescending(pt => pt.Date)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<PlayerTime>> ListPlayerTimesForCompletionsAsync(
        int auth, CancellationToken ct) =>
        await playerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .ToListAsync(ct);

    private async Task<Dictionary<string, int>> GetTiersByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct)
    {
        if (mapNames.Count == 0) return new Dictionary<string, int>();
        return await mapTiers
            .Where(mt => mapNames.Contains(mt.Map))
            .ToDictionaryAsync(mt => mt.Map, mt => mt.Tier, ct);
    }

    private async Task<IReadOnlyList<(string Map, float MinTime)>> GetMinTimesByMapForCompletionsAsync(
        IReadOnlyList<string> maps, CancellationToken ct)
    {
        if (maps.Count == 0) return [];
        var rows = await playerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0 && maps.Contains(pt.Map))
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(pt => pt.Time) })
            .ToListAsync(ct);
        return rows.Select(x => (x.Map, x.MinTime)).ToList();
    }

    private Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct) =>
        users.CountAsync(u => u.Points > points || (u.Points == points && u.Auth < auth), ct);

    private Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct) =>
        users.CountAsync(u => u.Playtime > playtime || (u.Playtime == playtime && u.Auth < auth), ct);

    private Task<int> CountCompletionRankingsAheadAsync(int completions, int auth, CancellationToken ct)
    {
        var ranked = playerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, Count = g.Select(x => x.Map).Distinct().Count() });
        return ranked.CountAsync(
            x => x.Count > completions || (x.Count == completions && x.Auth < auth),
            ct);
    }
}
