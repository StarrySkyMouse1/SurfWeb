using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.DomainServices;

public interface ICompletionPolicy
{
    RunKind ResolveRunKind(TrackId trackId, StageId? stageId);
}
