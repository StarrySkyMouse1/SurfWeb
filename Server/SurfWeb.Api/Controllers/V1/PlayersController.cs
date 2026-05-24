using Microsoft.AspNetCore.Mvc;
using SurfWeb.Application.Common;
using SurfWeb.Application.Abstractions;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayersController(IPlayerQueryService players) : ControllerBase
{
    [HttpGet("{auth:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Get(int auth, CancellationToken ct)
    {
        var player = await players.GetPlayerAsync(auth, ct);
        if (player is null) return NotFound(ApiResponse<object>.Fail("not_found", $"Player {auth} not found."));
        return Ok(ApiResponse<object>.Ok(player));
    }

    [HttpGet("{auth:int}/times")]
    public async Task<ActionResult<ApiResponse<object>>> Times(
        int auth,
        [FromQuery] string? map,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await players.GetPlayerTimesAsync(auth, map, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(items.Cast<object>().ToList(), new ApiMeta(page, pageSize, total)));
    }

    [HttpGet("{auth:int}/completions")]
    public async Task<ActionResult<ApiResponse<object>>> Completions(
        int auth,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var player = await players.GetPlayerAsync(auth, ct);
        if (player is null) return NotFound(ApiResponse<object>.Fail("not_found", $"Player {auth} not found."));

        var (items, total) = await players.GetPlayerCompletionsAsync(auth, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(items.Cast<object>().ToList(), new ApiMeta(page, pageSize, total)));
    }
}
