namespace SurfWeb.Domain.ReadModels;

public sealed record LeaderboardBestTime(
    int Auth,
    float MinTime,
    int SourceId,
    float? Sync,
    int? Jumps,
    int? Date);
