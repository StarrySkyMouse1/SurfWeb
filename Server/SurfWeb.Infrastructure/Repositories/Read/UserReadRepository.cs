using Microsoft.EntityFrameworkCore;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Entities;
using SurfWeb.Infrastructure.Persistence;

namespace SurfWeb.Infrastructure.Repositories.Read;

public sealed class UserReadRepository(ShavitDbContext db) : IUserReadRepository
{
    public Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Auth == auth, ct);

    public Task<int> CountAllAsync(CancellationToken ct = default) =>
        db.Users.CountAsync(ct);

    public async Task<IReadOnlyList<User>> ListOrderedByPointsAsync(int skip, int take, CancellationToken ct = default) =>
        await db.Users
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Auth)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> ListOrderedByPlaytimeAsync(int skip, int take, CancellationToken ct = default) =>
        await db.Users
            .OrderByDescending(u => u.Playtime)
            .ThenBy(u => u.Auth)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct = default) =>
        db.Users.CountAsync(u => u.Points > points || (u.Points == points && u.Auth < auth), ct);

    public Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct = default) =>
        db.Users.CountAsync(u => u.Playtime > playtime || (u.Playtime == playtime && u.Auth < auth), ct);

    public async Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(
        IReadOnlyList<int> authIds, CancellationToken ct = default)
    {
        if (authIds.Count == 0) return new Dictionary<int, string?>();
        return await db.Users
            .Where(u => authIds.Contains(u.Auth))
            .ToDictionaryAsync(u => u.Auth, u => u.Name, ct);
    }

    public Task<string?> GetNameAsync(int auth, CancellationToken ct = default) =>
        db.Users.Where(u => u.Auth == auth).Select(u => u.Name).FirstOrDefaultAsync(ct);

    public async Task<Dictionary<string, int>> GetAuthsByNamesAsync(
        IReadOnlyList<string> names, CancellationToken ct = default)
    {
        if (names.Count == 0) return new Dictionary<string, int>();
        var distinct = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        if (distinct.Count == 0) return new Dictionary<string, int>();

        var users = await db.Users
            .Where(u => u.Name != null && distinct.Contains(u.Name))
            .Select(u => new { u.Name, u.Auth })
            .ToListAsync(ct);

        return users
            .Where(u => u.Name is not null)
            .ToDictionary(u => u.Name!, u => u.Auth);
    }
}
