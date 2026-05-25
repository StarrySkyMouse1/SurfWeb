using SurfWeb.Data.Dtos;

namespace SurfWeb.Services.IServices;

public interface IRankingService
{
    Task<(IReadOnlyList<RankingEntryDto> Items, int Total)> GetRankingsAsync(
        string type, int page, int pageSize, CancellationToken ct = default);
}
