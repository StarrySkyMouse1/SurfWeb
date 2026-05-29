using Microsoft.EntityFrameworkCore;
using SurfWeb.Core.Models;
using SurfWeb.Repositories;
using SurfWeb.Services.IServices;

namespace SurfWeb.Services;

public sealed class UserService(IBaseRepository<User> users) : IUserService
{
    public async Task<IReadOnlyDictionary<string, int>> GetAuthsByNamesAsync(
        IReadOnlyList<string> playerNames,
        CancellationToken ct = default)
    {
        var distinct = playerNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();
        if (distinct.Count == 0)
            return new Dictionary<string, int>();

        var matched = await users
            .Where(u => u.Name != null && distinct.Contains(u.Name))
            .Select(u => new { u.Name, u.Auth })
            .ToListAsync(ct);

        return matched
            .Where(u => u.Name is not null)
            .ToDictionary(u => u.Name!, u => u.Auth);
    }
}
