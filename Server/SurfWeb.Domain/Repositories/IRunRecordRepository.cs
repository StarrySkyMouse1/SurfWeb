using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Repositories;

public interface IRunRecordRepository
{
    Task<RunRecord?> GetBestAsync(
        MapName mapName,
        StyleId styleId,
        TrackId trackId,
        StageId? stageId,
        RunKind kind,
        CancellationToken ct = default);

    Task AddAsync(RunRecord runRecord, CancellationToken ct = default);
}
