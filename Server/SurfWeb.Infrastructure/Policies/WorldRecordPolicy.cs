using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.DomainServices;

namespace SurfWeb.Infrastructure.Policies;

public sealed class WorldRecordPolicy : IWorldRecordPolicy
{
    public bool IsWorldRecord(RunRecord candidate, RunRecord? currentRecord) =>
        currentRecord is null || candidate.Time.IsFasterThan(currentRecord.Time);
}
