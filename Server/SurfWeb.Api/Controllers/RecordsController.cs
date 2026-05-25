using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/records")]
public sealed class RecordsController(IRecordService records) : ControllerBase
{
    /// <summary>
    /// 获取最新完成记录。
    /// </summary>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="limit">兼容旧参数：若指定则覆盖 pageSize。</param>
    /// <param name="filter">筛选：main（主线）、bonus（奖励赛道）、wr（打破 WR）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>记录列表与分页 meta。</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<object>>> Recent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? limit = null,
        [FromQuery] string? filter = null,
        CancellationToken ct = default)
    {
        if (limit.HasValue)
            pageSize = limit.Value;
        var (items, total) = await records.GetRecentAsync(page, pageSize, filter, ct);
        return Ok(ApiResponse<object>.Ok(items, new ApiMeta(page, pageSize, total)));
    }
}