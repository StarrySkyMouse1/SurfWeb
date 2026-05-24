using SurfWeb.Application.Dtos;

namespace SurfWeb.Application.Abstractions;

public interface IMapQueryService
{
    Task<(IReadOnlyList<MapListItemDto> Items, int Total)> GetMapsAsync(
        int? tier, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<MapDetailDto?> GetMapAsync(string mapName, CancellationToken ct = default);

    Task<(IReadOnlyList<LeaderboardEntryDto> Items, int Total)> GetLeaderboardAsync(
        string mapName, byte track, byte? stage, int page, int pageSize, CancellationToken ct = default);
}

public interface IPlayerQueryService
{
    Task<PlayerSummaryDto?> GetPlayerAsync(int auth, CancellationToken ct = default);

    Task<(IReadOnlyList<PlayerTimeDto> Items, int Total)> GetPlayerTimesAsync(
        int auth, string? map, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PlayerCompletionDto> Items, int Total)> GetPlayerCompletionsAsync(
        int auth, int page, int pageSize, CancellationToken ct = default);
}

public interface IRankingQueryService
{
    Task<(IReadOnlyList<RankingEntryDto> Items, int Total)> GetRankingsAsync(
        string type, int page, int pageSize, CancellationToken ct = default);
}

public interface IRecordQueryService
{
    Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
        int page, int pageSize, CancellationToken ct = default);
}
