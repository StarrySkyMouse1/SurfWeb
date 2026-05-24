using SurfWeb.Domain.Aggregates.Maps;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Repositories;

public interface IMapRepository
{
    Task<Map?> GetByIdAsync(MapName mapName, CancellationToken ct = default);

    Task SaveAsync(Map map, CancellationToken ct = default);
}
