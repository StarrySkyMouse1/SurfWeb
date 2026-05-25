using SurfWeb.Data.Dtos;

namespace SurfWeb.Services.IServices;

public interface IMapService
{
    Task<(IReadOnlyList<MapListItemDto> Items, int Total)> GetMapsAsync(
        int? tier, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<MapDetailDto?> GetMapAsync(string mapName, CancellationToken ct = default);

    Task<(IReadOnlyList<LeaderboardEntryDto> Items, int Total)> GetLeaderboardAsync(
        string mapName, byte track, byte? stage, int page, int pageSize, CancellationToken ct = default);
}
