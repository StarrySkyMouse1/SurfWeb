namespace SurfWeb.Core.Dtos;

public sealed record MapCheckpointSeriesDto(
    int Rank,
    int Auth,
    string? PlayerName,
    /// <summary>与 <see cref="MapCheckpointChartDto.CheckpointLabels"/> 等长；无该检查点时为 null。</summary>
    IReadOnlyList<float?> CumulativeSeconds);

public sealed record MapCheckpointChartDto(
    IReadOnlyList<string> CheckpointLabels,
    IReadOnlyList<MapCheckpointSeriesDto> Series);
