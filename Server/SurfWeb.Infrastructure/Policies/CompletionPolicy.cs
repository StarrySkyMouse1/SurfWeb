using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.DomainServices;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Infrastructure.Policies;

public sealed class CompletionPolicy : ICompletionPolicy
{
    public RunKind ResolveRunKind(TrackId trackId, StageId? stageId) =>
        stageId is not null || trackId.Value > 0
            ? RunKind.Stage
            : RunKind.Completion;
}
