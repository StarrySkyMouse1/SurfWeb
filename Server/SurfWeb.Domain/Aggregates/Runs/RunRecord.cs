using SurfWeb.Domain.Common;
using SurfWeb.Domain.Events;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Aggregates.Runs;

public sealed class RunRecord : AggregateRoot<Guid>
{
    private RunRecord(
        Guid id,
        PlayerId playerId,
        MapName mapName,
        StyleId styleId,
        TrackId trackId,
        StageId? stageId,
        RunTime time,
        RunKind kind,
        DateTime recordedAtUtc) : base(id)
    {
        if (kind == RunKind.Stage && stageId is null)
        {
            throw new ArgumentException("Stage runs must include a stage id.", nameof(stageId));
        }

        if (kind == RunKind.Completion && stageId is not null)
        {
            throw new ArgumentException("Completion runs cannot include a stage id.", nameof(stageId));
        }

        PlayerId = playerId;
        MapName = mapName;
        StyleId = styleId;
        TrackId = trackId;
        StageId = stageId;
        Time = time;
        Kind = kind;
        RecordedAtUtc = recordedAtUtc;
    }

    public PlayerId PlayerId { get; }

    public MapName MapName { get; }

    public StyleId StyleId { get; }

    public TrackId TrackId { get; }

    public StageId? StageId { get; }

    public RunTime Time { get; }

    public RunKind Kind { get; }

    public DateTime RecordedAtUtc { get; }

    public static RunRecord Create(
        PlayerId playerId,
        MapName mapName,
        StyleId styleId,
        TrackId trackId,
        StageId? stageId,
        RunTime time,
        RunKind kind,
        DateTime recordedAtUtc)
    {
        var runRecord = new RunRecord(
            Guid.NewGuid(),
            playerId,
            mapName,
            styleId,
            trackId,
            stageId,
            time,
            kind,
            recordedAtUtc);

        runRecord.RaiseDomainEvent(new RunRecordedDomainEvent(
            runRecord.Id,
            runRecord.PlayerId,
            runRecord.MapName,
            runRecord.StyleId,
            runRecord.TrackId,
            runRecord.StageId,
            runRecord.Kind,
            runRecord.Time,
            runRecord.RecordedAtUtc));

        return runRecord;
    }

    public static RunRecord Rehydrate(
        Guid id,
        PlayerId playerId,
        MapName mapName,
        StyleId styleId,
        TrackId trackId,
        StageId? stageId,
        RunTime time,
        RunKind kind,
        DateTime recordedAtUtc) =>
        new(
            id,
            playerId,
            mapName,
            styleId,
            trackId,
            stageId,
            time,
            kind,
            recordedAtUtc);
}
