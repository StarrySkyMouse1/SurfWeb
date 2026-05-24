using Microsoft.EntityFrameworkCore;
using SurfWeb.Domain.Aggregates.Players;
using SurfWeb.Domain.Repositories;
using SurfWeb.Domain.ValueObjects;
using SurfWeb.Infrastructure.Persistence;

namespace SurfWeb.Infrastructure.Repositories.Write;

public sealed class PlayerRepository(ShavitDbContext db) : IPlayerRepository
{
    public async Task<Player?> GetByIdAsync(PlayerId playerId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Auth == playerId.Value, ct);
        if (user is null)
        {
            return null;
        }

        var displayName = string.IsNullOrWhiteSpace(user.Name)
            ? $"Player {user.Auth}"
            : user.Name;

        return Player.Create(playerId, displayName);
    }

    public Task SaveAsync(Player player, CancellationToken ct = default) => Task.CompletedTask;
}
