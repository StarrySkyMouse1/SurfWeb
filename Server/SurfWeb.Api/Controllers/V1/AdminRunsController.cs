using Microsoft.AspNetCore.Mvc;
using SurfWeb.Application.Commands.RecordRun;
using SurfWeb.Application.Common;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/admin/runs")]
public sealed class AdminRunsController(IRecordRunUseCase recordRunUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RecordRunResponseDto>>> Record(
        [FromBody] RecordRunRequestDto request,
        CancellationToken ct = default)
    {
        var result = await recordRunUseCase.ExecuteAsync(
            new RecordRunCommand(
                new PlayerId(request.PlayerId),
                request.PlayerDisplayName,
                new MapName(request.MapName),
                new StyleId(request.StyleId),
                new TrackId(request.TrackId),
                request.StageId is null ? null : new StageId(request.StageId.Value),
                new RunTime(request.TimeSeconds),
                request.RecordedAtUtc),
            ct);

        return Ok(ApiResponse<RecordRunResponseDto>.Ok(
            new RecordRunResponseDto(result.RunId, result.RunKind.ToString(), result.IsWorldRecord)));
    }
}

public sealed record RecordRunRequestDto(
    int PlayerId,
    string PlayerDisplayName,
    string MapName,
    byte StyleId,
    byte TrackId,
    byte? StageId,
    double TimeSeconds,
    DateTime RecordedAtUtc);

public sealed record RecordRunResponseDto(
    Guid RunId,
    string RunKind,
    bool IsWorldRecord);
