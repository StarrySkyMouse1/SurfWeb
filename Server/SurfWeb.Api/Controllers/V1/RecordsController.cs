using Microsoft.AspNetCore.Mvc;
using SurfWeb.Application.Common;
using SurfWeb.Application.Abstractions;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/records")]
public sealed class RecordsController(IRecordQueryService records) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<object>>> Recent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? limit = null,
        CancellationToken ct = default)
    {
        if (limit.HasValue)
            pageSize = limit.Value;
        var (items, total) = await records.GetRecentAsync(page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(items, new ApiMeta(page, pageSize, total)));
    }
}
