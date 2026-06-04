using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
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
    /// <param name="filter">筛选：all / main / stage / bonus / wr。</param>
    /// <param name="wrScope">仅 filter=wr：main / stage / bonus。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>记录列表与分页 meta。</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecentRecordDto>>>> Recent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? limit = null,
        [FromQuery] RecentRecordFilter filter = RecentRecordFilter.All,
        [FromQuery] WrRankingScope wrScope = WrRankingScope.Main,
        CancellationToken ct = default)
    {
        if (limit.HasValue)
            pageSize = limit.Value;
        var (items, total) = await records.GetRecentAsync(page, pageSize, filter, wrScope, ct);
        return Ok(ApiResponse<IReadOnlyList<RecentRecordDto>>.Ok(items, new ApiMeta(page, pageSize, total)));
    }
}
