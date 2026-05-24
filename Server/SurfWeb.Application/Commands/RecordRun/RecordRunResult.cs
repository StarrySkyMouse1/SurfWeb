using SurfWeb.Domain.Aggregates.Runs;

namespace SurfWeb.Application.Commands.RecordRun;

public sealed record RecordRunResult(
    Guid RunId,
    RunKind RunKind,
    bool IsWorldRecord);
