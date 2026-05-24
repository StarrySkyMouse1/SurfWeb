using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.Common;
using SurfWeb.Domain.Events;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Aggregates.Maps;

public sealed class Map : AggregateRoot<MapName>
{
    private readonly Dictionary<WorldRecordSlot, Guid> _worldRecordRunIds = [];

    private Map(MapName id) : base(id)
    {
    }

    public static Map Create(MapName mapName) => new(mapName);

    public void RecordWorldRecord(RunRecord candidate, RunRecord? previousRecord)
    {
        if (candidate.MapName != Id)
        {
            throw new InvalidOperationException("Run belongs to a different map.");
        }

        _worldRecordRunIds[new WorldRecordSlot(candidate.StyleId, candidate.TrackId, candidate.StageId, candidate.Kind)] = candidate.Id;

        RaiseDomainEvent(new WorldRecordBrokenDomainEvent(
            candidate.Id,
            candidate.MapName,
            candidate.PlayerId,
            candidate.StyleId,
            candidate.TrackId,
            candidate.StageId,
            candidate.Kind,
            candidate.Time,
            previousRecord?.PlayerId,
            previousRecord?.Time,
            candidate.RecordedAtUtc));
    }

    private readonly record struct WorldRecordSlot(StyleId StyleId, TrackId TrackId, StageId? StageId, RunKind Kind);
}
