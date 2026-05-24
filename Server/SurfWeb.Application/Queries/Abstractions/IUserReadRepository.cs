using SurfWeb.Domain.Entities;

namespace SurfWeb.Application.Queries.Abstractions;

public interface IUserReadRepository
{
    Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default);

    Task<int> CountAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<User>> ListOrderedByPointsAsync(int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<User>> ListOrderedByPlaytimeAsync(int skip, int take, CancellationToken ct = default);

    Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct = default);

    Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct = default);

    Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(
        IReadOnlyList<int> authIds, CancellationToken ct = default);

    Task<string?> GetNameAsync(int auth, CancellationToken ct = default);

    Task<Dictionary<string, int>> GetAuthsByNamesAsync(
        IReadOnlyList<string> names, CancellationToken ct = default);
}
