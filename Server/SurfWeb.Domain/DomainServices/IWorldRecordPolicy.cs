using SurfWeb.Domain.Aggregates.Runs;

namespace SurfWeb.Domain.DomainServices;

public interface IWorldRecordPolicy
{
    bool IsWorldRecord(RunRecord candidate, RunRecord? currentRecord);
}
