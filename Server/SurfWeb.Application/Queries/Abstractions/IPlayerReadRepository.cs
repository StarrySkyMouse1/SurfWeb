using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;

namespace SurfWeb.Application.Queries.Abstractions;

public interface IPlayerReadRepository
{
    Task<int> CountDistinctMapCompletionsAsync(int auth, CancellationToken ct = default);

    Task<int> CountPlayerTimesAsync(int auth, string? map, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerTime>> ListPlayerTimesPageAsync(
        int auth, string? map, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerTime>> ListPlayerTimesForCompletionsAsync(
        int auth, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerTime>> ScanRecentPlayerTimesAsync(
        int take, CancellationToken ct = default);

    Task<IReadOnlyList<(string Map, byte Track, float MinTime)>> GetMinTimesByMapTrackAsync(
        IReadOnlyList<string> maps, CancellationToken ct = default);

    Task<IReadOnlyList<(string Map, float MinTime)>> GetMinTimesByMapForCompletionsAsync(
        IReadOnlyList<string> maps, CancellationToken ct = default);

    Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(CancellationToken ct = default);

    Task<int> CountCompletionRankingsAheadAsync(
        int completions, int auth, CancellationToken ct = default);
}
