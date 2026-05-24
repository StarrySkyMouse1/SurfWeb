using SurfWeb.Domain.Aggregates.Players;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(PlayerId playerId, CancellationToken ct = default);

    Task SaveAsync(Player player, CancellationToken ct = default);
}
