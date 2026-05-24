using Microsoft.EntityFrameworkCore;
using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.Repositories;
using SurfWeb.Domain.ValueObjects;
using SurfWeb.Infrastructure.Persistence;

namespace SurfWeb.Infrastructure.Repositories.Write;

public sealed class RunRecordRepository(ShavitDbContext db) : IRunRecordRepository
{
    public async Task<RunRecord?> GetBestAsync(
        MapName mapName,
        StyleId styleId,
        TrackId trackId,
        StageId? stageId,
        RunKind kind,
        CancellationToken ct = default)
    {
        if (kind == RunKind.Stage && stageId is not null)
        {
            var stageRow = await db.StageTimes
                .Where(x =>
                    x.Map == mapName.Value &&
                    x.Track == trackId.Value &&
                    x.Stage == stageId.Value.Value)
                .OrderBy(x => x.Time)
                .FirstOrDefaultAsync(ct);

            return stageRow is null
                ? null
                : RunRecord.Rehydrate(
                    Guid.NewGuid(),
                    new PlayerId(stageRow.Auth),
                    new MapName(stageRow.Map),
                    new StyleId(stageRow.Style),
                    new TrackId(stageRow.Track),
                    new StageId(stageRow.Stage),
                    new RunTime(stageRow.Time),
                    RunKind.Stage,
                    DateTime.SpecifyKind(DateTime.UnixEpoch.AddSeconds(stageRow.Date ?? 0), DateTimeKind.Utc));
        }

        var playerTimeRow = await db.PlayerTimes
            .Where(x =>
                x.Map == mapName.Value &&
                x.Track == trackId.Value &&
                x.Auth != null)
            .OrderBy(x => x.Time)
            .FirstOrDefaultAsync(ct);

        return playerTimeRow is null
            ? null
            : RunRecord.Rehydrate(
                Guid.NewGuid(),
                new PlayerId(playerTimeRow.Auth!.Value),
                new MapName(playerTimeRow.Map),
                new StyleId(playerTimeRow.Style),
                new TrackId(playerTimeRow.Track),
                null,
                new RunTime(playerTimeRow.Time),
                RunKind.Completion,
                DateTime.SpecifyKind(DateTime.UnixEpoch.AddSeconds(playerTimeRow.Date ?? 0), DateTimeKind.Utc));
    }

    public Task AddAsync(RunRecord runRecord, CancellationToken ct = default) => Task.CompletedTask;
}
