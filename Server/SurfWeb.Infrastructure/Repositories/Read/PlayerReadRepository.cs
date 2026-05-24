using Microsoft.EntityFrameworkCore;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;
using SurfWeb.Infrastructure.Persistence;

namespace SurfWeb.Infrastructure.Repositories.Read;

public sealed class PlayerReadRepository(ShavitDbContext db) : IPlayerReadRepository
{
    public Task<int> CountDistinctMapCompletionsAsync(int auth, CancellationToken ct = default) =>
        db.PlayerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .Select(pt => pt.Map)
            .Distinct()
            .CountAsync(ct);

    public Task<int> CountPlayerTimesAsync(int auth, string? map, CancellationToken ct = default)
    {
        var query = db.PlayerTimes.Where(pt => pt.Auth == auth);
        if (!string.IsNullOrWhiteSpace(map))
            query = query.Where(pt => pt.Map == map);
        return query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerTime>> ListPlayerTimesPageAsync(
        int auth, string? map, int skip, int take, CancellationToken ct = default)
    {
        var query = db.PlayerTimes.Where(pt => pt.Auth == auth);
        if (!string.IsNullOrWhiteSpace(map))
            query = query.Where(pt => pt.Map == map);
        return await query
            .OrderByDescending(pt => pt.Date)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerTime>> ListPlayerTimesForCompletionsAsync(
        int auth, CancellationToken ct = default) =>
        await db.PlayerTimes
            .Where(pt => pt.Auth == auth && pt.Track == 0)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerTime>> ScanRecentPlayerTimesAsync(
        int take, CancellationToken ct = default) =>
        await db.PlayerTimes
            .Where(pt => pt.Auth != null && pt.Date != null)
            .OrderByDescending(pt => pt.Date)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(string Map, byte Track, float MinTime)>> GetMinTimesByMapTrackAsync(
        IReadOnlyList<string> maps, CancellationToken ct = default)
    {
        if (maps.Count == 0) return [];
        var rows = await db.PlayerTimes
            .Where(pt => pt.Auth != null && maps.Contains(pt.Map))
            .GroupBy(pt => new { pt.Map, pt.Track, pt.Auth })
            .Select(g => new { g.Key.Map, g.Key.Track, MinTime = g.Min(pt => pt.Time) })
            .ToListAsync(ct);
        return rows.Select(x => (x.Map, x.Track, x.MinTime)).ToList();
    }

    public async Task<IReadOnlyList<(string Map, float MinTime)>> GetMinTimesByMapForCompletionsAsync(
        IReadOnlyList<string> maps, CancellationToken ct = default)
    {
        if (maps.Count == 0) return [];
        var rows = await db.PlayerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0 && maps.Contains(pt.Map))
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(pt => pt.Time) })
            .ToListAsync(ct);
        return rows.Select(x => (x.Map, x.MinTime)).ToList();
    }

    public async Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(
        CancellationToken ct = default)
    {
        var ranked = await db.PlayerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, Count = g.Select(x => x.Map).Distinct().Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Auth)
            .ToListAsync(ct);
        return ranked.Select(x => new CompletionRankEntry(x.Auth, x.Count)).ToList();
    }

    public Task<int> CountCompletionRankingsAheadAsync(
        int completions, int auth, CancellationToken ct = default)
    {
        var ranked = db.PlayerTimes
            .Where(pt => pt.Auth != null && pt.Track == 0)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, Count = g.Select(x => x.Map).Distinct().Count() });
        return ranked.CountAsync(
            x => x.Count > completions || (x.Count == completions && x.Auth < auth),
            ct);
    }
}
