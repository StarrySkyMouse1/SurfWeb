using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Application.Commands.RecordRun;

public sealed record RecordRunCommand(
    PlayerId PlayerId,
    string PlayerDisplayName,
    MapName MapName,
    StyleId StyleId,
    TrackId TrackId,
    StageId? StageId,
    RunTime Time,
    DateTime RecordedAtUtc);
