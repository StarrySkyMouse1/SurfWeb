using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;

namespace SurfWeb.Application.Queries.Abstractions;

public interface IMapReadRepository
{
    Task<(IReadOnlyList<MapTier> Maps, int Total)> ListMapTiersAsync(
        int? tier, string? search, int skip, int take, CancellationToken ct = default);

    Task<MapTier?> FindMapTierAsync(string mapName, CancellationToken ct = default);

    Task<Dictionary<string, int>> GetTiersByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct = default);

    Task<int> CountDistinctCompletionsAsync(string mapName, CancellationToken ct = default);

    Task<(float Time, int Auth)?> GetMainWorldRecordAsync(string mapName, CancellationToken ct = default);

    Task<IReadOnlyList<byte>> GetBonusTrackIdsAsync(string mapName, CancellationToken ct = default);

    Task<Dictionary<string, int>> GetCompletionCountsByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct = default);

    Task<IReadOnlyList<MapWorldRecord>> GetWorldRecordsByMapsAsync(
        IReadOnlyList<string> mapNames, CancellationToken ct = default);

    Task<int> CountLeaderboardPlayerTimesAsync(
        string mapName, byte track, CancellationToken ct = default);

    Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardPlayerTimePageAsync(
        string mapName, byte track, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerTime>> GetPlayerTimeRowsForLeaderboardAsync(
        string mapName, byte track, IReadOnlyList<int> authIds, CancellationToken ct = default);

    Task<int> CountLeaderboardStageTimesAsync(
        string mapName, byte track, byte stage, CancellationToken ct = default);

    Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardStageTimePageAsync(
        string mapName, byte track, byte stage, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<StageTime>> GetStageTimeRowsForLeaderboardAsync(
        string mapName, byte track, byte stage, IReadOnlyList<int> authIds, CancellationToken ct = default);
}
