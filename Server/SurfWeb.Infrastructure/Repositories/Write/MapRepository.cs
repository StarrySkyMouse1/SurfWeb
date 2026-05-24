using Microsoft.EntityFrameworkCore;
using SurfWeb.Domain.Aggregates.Maps;
using SurfWeb.Domain.Repositories;
using SurfWeb.Domain.ValueObjects;
using SurfWeb.Infrastructure.Persistence;

namespace SurfWeb.Infrastructure.Repositories.Write;

public sealed class MapRepository(ShavitDbContext db) : IMapRepository
{
    public async Task<Map?> GetByIdAsync(MapName mapName, CancellationToken ct = default)
    {
        var mapTier = await db.MapTiers.FirstOrDefaultAsync(x => x.Map == mapName.Value, ct);
        return mapTier is null ? null : Map.Create(new MapName(mapTier.Map));
    }

    public Task SaveAsync(Map map, CancellationToken ct = default) => Task.CompletedTask;
}
