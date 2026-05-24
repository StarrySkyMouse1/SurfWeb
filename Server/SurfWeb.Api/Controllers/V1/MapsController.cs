using Microsoft.AspNetCore.Mvc;
using SurfWeb.Application.Common;
using SurfWeb.Application.Abstractions;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/maps")]
public sealed class MapsController(IMapQueryService maps) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<object>>>> List(
        [FromQuery] int? tier,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
    {
        var (items, total) = await maps.GetMapsAsync(tier, search, page, pageSize, ct);
        return Ok(ApiResponse<IReadOnlyList<object>>.Ok(items.Cast<object>().ToList(),
            new ApiMeta(page, pageSize, total)));
    }

    [HttpGet("{mapName}")]
    public async Task<ActionResult<ApiResponse<object>>> Detail(
        string mapName,
        CancellationToken ct = default)
    {
        var map = await maps.GetMapAsync(mapName, ct);
        if (map is null) return NotFound(ApiResponse<object>.Fail("not_found", $"Map '{mapName}' not found."));
        return Ok(ApiResponse<object>.Ok(map));
    }

    [HttpGet("{mapName}/leaderboard")]
    public async Task<ActionResult<ApiResponse<object>>> Leaderboard(
        string mapName,
        [FromQuery] byte track = 0,
        [FromQuery] byte? stage = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var (items, total) = await maps.GetLeaderboardAsync(mapName, track, stage, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(items.Cast<object>().ToList(), new ApiMeta(page, pageSize, total)));
    }
}
