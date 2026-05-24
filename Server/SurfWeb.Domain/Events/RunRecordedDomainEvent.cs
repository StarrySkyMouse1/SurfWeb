using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Events;

public sealed record RunRecordedDomainEvent(
    Guid RunId,
    PlayerId PlayerId,
    MapName MapName,
    StyleId StyleId,
    TrackId TrackId,
    StageId? StageId,
    RunKind RunKind,
    RunTime Time,
    DateTime OccurredOnUtc) : IDomainEvent;
