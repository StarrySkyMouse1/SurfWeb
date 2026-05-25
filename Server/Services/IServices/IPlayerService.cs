using SurfWeb.Data.Dtos;

namespace SurfWeb.Services.IServices;

public interface IPlayerService
{
    Task<PlayerSummaryDto?> GetPlayerAsync(int auth, CancellationToken ct = default);

    Task<(IReadOnlyList<PlayerTimeDto> Items, int Total)> GetPlayerTimesAsync(
        int auth, string? map, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PlayerCompletionDto> Items, int Total)> GetPlayerCompletionsAsync(
        int auth, int page, int pageSize, CancellationToken ct = default);
}
