using SurfWeb.Application.Abstractions;
using SurfWeb.Domain.Aggregates.Maps;
using SurfWeb.Domain.Aggregates.Players;
using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.DomainServices;
using SurfWeb.Domain.Repositories;

namespace SurfWeb.Application.Commands.RecordRun;

public sealed class RecordRunUseCase(
    IPlayerRepository playerRepository,
    IMapRepository mapRepository,
    IRunRecordRepository runRecordRepository,
    IWorldRecordPolicy worldRecordPolicy,
    ICompletionPolicy completionPolicy,
    IUnitOfWork unitOfWork) : IRecordRunUseCase
{
    public async Task<RecordRunResult> ExecuteAsync(RecordRunCommand command, CancellationToken ct = default)
    {
        var player = await playerRepository.GetByIdAsync(command.PlayerId, ct)
            ?? Player.Create(command.PlayerId, command.PlayerDisplayName);
        var map = await mapRepository.GetByIdAsync(command.MapName, ct)
            ?? Map.Create(command.MapName);

        var runKind = completionPolicy.ResolveRunKind(command.TrackId, command.StageId);
        if (runKind == RunKind.Completion)
        {
            player.RegisterCompletion(command.MapName, command.StyleId);
        }

        var runRecord = RunRecord.Create(
            command.PlayerId,
            command.MapName,
            command.StyleId,
            command.TrackId,
            command.StageId,
            command.Time,
            runKind,
            command.RecordedAtUtc);
        var currentRecord = await runRecordRepository.GetBestAsync(
            command.MapName,
            command.StyleId,
            command.TrackId,
            command.StageId,
            runKind,
            ct);

        var isWorldRecord = worldRecordPolicy.IsWorldRecord(runRecord, currentRecord);
        if (isWorldRecord)
        {
            map.RecordWorldRecord(runRecord, currentRecord);
        }

        await playerRepository.SaveAsync(player, ct);
        await mapRepository.SaveAsync(map, ct);
        await runRecordRepository.AddAsync(runRecord, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new RecordRunResult(runRecord.Id, runKind, isWorldRecord);
    }
}
