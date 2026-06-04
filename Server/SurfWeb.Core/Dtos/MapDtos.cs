namespace SurfWeb.Core.Dtos;

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
    IReadOnlyList<byte> BonusTracks,
    IReadOnlyList<byte> Stages);

public sealed record LeaderboardEntryDto(
    int Rank,
    int Auth,
    string? PlayerName,
    float Time,
    string TimeFormatted,
    float? Sync,
    int? Jumps,
    DateTimeOffset? Date);

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
    float? GapFromWr,
    byte? Stage = null,
    int? Tier = null);

public sealed record MapImageConfigDto(string? BaseUrl, string Extension);
