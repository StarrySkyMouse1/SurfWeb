using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Events;

public sealed record WorldRecordBrokenDomainEvent(
    Guid RunId,
    MapName MapName,
    PlayerId PlayerId,
    StyleId StyleId,
    TrackId TrackId,
    StageId? StageId,
    RunKind RunKind,
    RunTime Time,
    PlayerId? PreviousPlayerId,
    RunTime? PreviousTime,
    DateTime OccurredOnUtc) : IDomainEvent;
