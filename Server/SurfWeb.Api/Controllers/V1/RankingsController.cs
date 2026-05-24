using Microsoft.AspNetCore.Mvc;
using SurfWeb.Application.Common;
using SurfWeb.Application.Abstractions;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/rankings")]
public sealed class RankingsController(IRankingQueryService rankings) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] string type = "points",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await rankings.GetRankingsAsync(type, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(items, new ApiMeta(page, pageSize, total)));
    }
}
