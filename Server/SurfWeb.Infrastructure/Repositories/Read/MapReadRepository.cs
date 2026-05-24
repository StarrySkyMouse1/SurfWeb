using Microsoft.EntityFrameworkCore;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;
using SurfWeb.Infrastructure.Persistence;

namespace SurfWeb.Infrastructure.Repositories.Read;

public sealed class MapReadRepository(ShavitDbContext db) : IMapReadRepository
{
    public async Task<(IReadOnlyList<MapTier> Maps, int Total)> ListMapTiersAsync(
        int? tier, string? search, int skip, int take, CancellationToken ct = default)
    {
        var query = db.MapTiers.AsQueryable();
        if (tier.HasValue)
            query = query.Where(m => m.Tier == tier.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Map.Contains(search));

        var total = await query.CountAsync(ct);
        var maps = await query.OrderBy(m => m.Map).Skip(skip).Take(take).ToListAsync(ct);
        return (maps, total);
    }

    public Task<MapTier?> FindMapTierAsync(string mapName, CancellationToken ct = default) =>
        db.MapTiers.FirstOrDefaultAsync(m => m.Map == mapName, ct);

    public async Task<Dictionary<string, int>> GetTiersByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct = default)
    {
        if (mapNames.Count == 0) return new Dictionary<string, int>();
        return await db.MapTiers
            .Where(mt => mapNames.Contains(mt.Map))
            .ToDictionaryAsync(mt => mt.Map, mt => mt.Tier, ct);
    }

    public Task<int> CountDistinctCompletionsAsync(string mapName, CancellationToken ct = default) =>
        db.PlayerTimes
            .Where(pt => pt.Map == mapName && pt.Track == 0 && pt.Auth != null)
            .Select(pt => pt.Auth)
            .Distinct()
            .CountAsync(ct);

    public async Task<(float Time, int Auth)?> GetMainWorldRecordAsync(
        string mapName, CancellationToken ct = default)
    {
        var wr = await db.PlayerTimes
            .Where(pt => pt.Map == mapName && pt.Track == 0 && pt.Auth != null)
            .OrderBy(pt => pt.Time)
            .Select(pt => new { pt.Time, Auth = pt.Auth!.Value })
            .FirstOrDefaultAsync(ct);
        return wr is null ? null : (wr.Time, wr.Auth);
    }

    public async Task<IReadOnlyList<byte>> GetBonusTrackIdsAsync(
        string mapName, CancellationToken ct = default)
    {
        var tracks = await db.PlayerTimes
            .Where(pt => pt.Map == mapName && pt.Track > 0 && pt.Auth != null)
            .Select(pt => pt.Track)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
        return tracks;
    }

    public async Task<Dictionary<string, int>> GetCompletionCountsByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct = default)
    {
        if (mapNames.Count == 0) return new Dictionary<string, int>();

        var counts = await db.PlayerTimes
            .Where(pt => mapNames.Contains(pt.Map) && pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, Count = g.Select(x => x.Auth).Distinct().Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Map, x => x.Count);
    }

    public async Task<IReadOnlyList<MapWorldRecord>> GetWorldRecordsByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct = default)
    {
        if (mapNames.Count == 0) return [];

        var minTimesQuery = db.PlayerTimes
            .Where(pt => mapNames.Contains(pt.Map) && pt.Track == 0 && pt.Auth != null)
            .GroupBy(pt => pt.Map)
            .Select(g => new { Map = g.Key, MinTime = g.Min(x => x.Time) });

        var wrCandidates = await (
            from pt in db.PlayerTimes
            join min in minTimesQuery on pt.Map equals min.Map
            where pt.Track == 0 && pt.Time == min.MinTime && pt.Auth != null
            select new { pt.Map, pt.Time, Auth = pt.Auth!.Value, pt.Id }
        ).ToListAsync(ct);

        return wrCandidates
            .GroupBy(x => x.Map)
            .Select(g =>
            {
                var best = g.OrderBy(x => x.Id).First();
                return new MapWorldRecord(best.Map, best.Time, best.Auth, best.Id);
            })
            .ToList();
    }

    public Task<int> CountLeaderboardPlayerTimesAsync(
        string mapName, byte track, CancellationToken ct = default) =>
        db.PlayerTimes
            .Where(pt => pt.Map == mapName && pt.Track == track && pt.Auth != null)
            .GroupBy(pt => pt.Auth)
            .CountAsync(ct);

    public async Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardPlayerTimePageAsync(
        string mapName, byte track, int skip, int take, CancellationToken ct = default)
    {
        var page = await db.PlayerTimes
            .Where(pt => pt.Map == mapName && pt.Track == track && pt.Auth != null)
            .GroupBy(pt => pt.Auth)
            .Select(g => new { Auth = g.Key!.Value, MinTime = g.Min(x => x.Time) })
            .OrderBy(x => x.MinTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return page.Select(x => (x.Auth, x.MinTime)).ToList();
    }

    public async Task<IReadOnlyList<PlayerTime>> GetPlayerTimeRowsForLeaderboardAsync(
        string mapName, byte track, IReadOnlyList<int> authIds, CancellationToken ct = default)
    {
        if (authIds.Count == 0) return [];
        return await db.PlayerTimes
            .Where(pt => pt.Map == mapName && pt.Track == track
                && pt.Auth != null && authIds.Contains(pt.Auth.Value))
            .ToListAsync(ct);
    }

    public Task<int> CountLeaderboardStageTimesAsync(
        string mapName, byte track, byte stage, CancellationToken ct = default) =>
        db.StageTimes
            .Where(st => st.Map == mapName && st.Track == track && st.Stage == stage)
            .GroupBy(st => st.Auth)
            .CountAsync(ct);

    public async Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardStageTimePageAsync(
        string mapName, byte track, byte stage, int skip, int take, CancellationToken ct = default)
    {
        var page = await db.StageTimes
            .Where(st => st.Map == mapName && st.Track == track && st.Stage == stage)
            .GroupBy(st => st.Auth)
            .Select(g => new { Auth = g.Key, MinTime = g.Min(x => x.Time) })
            .OrderBy(x => x.MinTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return page.Select(x => (x.Auth, x.MinTime)).ToList();
    }

    public async Task<IReadOnlyList<StageTime>> GetStageTimeRowsForLeaderboardAsync(
        string mapName, byte track, byte stage, IReadOnlyList<int> authIds, CancellationToken ct = default)
    {
        if (authIds.Count == 0) return [];
        return await db.StageTimes
            .Where(st => st.Map == mapName && st.Track == track && st.Stage == stage
                && authIds.Contains(st.Auth))
            .ToListAsync(ct);
    }
}
