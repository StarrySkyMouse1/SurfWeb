namespace SurfWeb.Application.Dtos;

public sealed record MapListItemDto(
    string Map,
    int Tier,
    int Completions,
    float? WorldRecordTime,
    string? WorldRecordTimeFormatted,
    string? WorldRecordPlayer);

public sealed record MapDetailDto(
    string Map,
    int Tier,
    float MaxVelocity,
    int Completions,
    float? WorldRecordTime,
    string? WorldRecordTimeFormatted,
    string? WorldRecordPlayer,
    int? WorldRecordAuth,
    IReadOnlyList<byte> BonusTracks);

public sealed record LeaderboardEntryDto(
    int Rank,
    int Auth,
    string? PlayerName,
    float Time,
    string TimeFormatted,
    float? Sync,
    int? Jumps,
    DateTimeOffset? Date);

public sealed record PlayerSummaryDto(
    int Auth,
    string? Name,
    float Points,
    float Playtime,
    int CompletionCount,
    int PointsRank,
    int PlaytimeRank,
    int CompletionRank);

public sealed record PlayerTimeDto(
    int Id,
    string Map,
    byte Style,
    byte Track,
    float Time,
    string TimeFormatted,
    float? Sync,
    DateTimeOffset? Date);

public sealed record PlayerCompletionDto(
    string Map,
    int? Tier,
    float Time,
    string TimeFormatted,
    byte Style,
    float? Sync,
    DateTimeOffset? Date,
    float? WorldRecordTime,
    float? GapFromWr);

public sealed record RankingEntryDto(int Rank, int Auth, string? Name, float Value);

public sealed record RecentRecordDto(
    int Id,
    int Auth,
    string? PlayerName,
    string Map,
    byte Style,
    byte Track,
    float Time,
    string TimeFormatted,
    DateTimeOffset? Date,
    float? WorldRecordTime,
    float? GapFromWr);

public sealed record StyleConfigDto(byte Id, string Name, bool Default);

public sealed record MapImageConfigDto(string? BaseUrl, string Extension);
