namespace SurfWeb.Core.Dtos;

public sealed record PlayerSummaryDto(
    int Auth,
    string? Name,
    float Points,
    int PointsRank,
    float Playtime,
    int PlaytimeRank,
    int MainCompletionCount,
    int MainCompletionRank,
    int BonusCompletionCount,
    int BonusCompletionRank,
    int WrCount,
    int WrRank,
    int MainWrCount,
    int MainWrRank,
    int StageWrCount,
    int StageWrRank,
    int BonusWrCount,
    int BonusWrRank);

public sealed record PlayerRecordDto(
    string Map,
    int? Tier,
    byte? Track,
    byte? Stage,
    float? Time,
    string? TimeFormatted,
    float? Sync,
    DateTimeOffset? Date,
    float? WorldRecordTime,
    float? GapFromWr,
    string? Status);

public sealed record PlayerChartBarDto(string Label, int Value);

public sealed record PlayerChartsDto(
    string PrimaryTitle,
    string TierTitle,
    IReadOnlyList<PlayerChartBarDto> PrimaryBars,
    IReadOnlyList<PlayerChartBarDto> TierBars,
    int RangeTotal,
    string? TopTierLabel,
    string? PrimaryFooterLeft,
    string? PrimaryFooterRight);

public sealed record PlayerRecordsPageDto(
    IReadOnlyList<PlayerRecordDto> Items,
    PlayerChartsDto Charts);
