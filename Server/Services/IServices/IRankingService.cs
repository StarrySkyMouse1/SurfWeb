using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;

namespace SurfWeb.Services.IServices;

public interface IRankingService
{
    Task<(IReadOnlyList<RankingEntryDto> Items, int Total)> GetRankingsAsync(
        RankingType type,
        int page,
        int pageSize,
        WrRankingScope wrScope = WrRankingScope.Main,
        TrackRankingScope completionScope = TrackRankingScope.Main,
        CancellationToken ct = default);
}
