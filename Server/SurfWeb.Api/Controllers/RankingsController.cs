using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/rankings")]
public sealed class RankingsController(IRankingService rankings) : ControllerBase
{
    /// <summary>
    /// 获取全站排行榜。
    /// </summary>
    /// <param name="type">排行类型，如 points。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>排行榜与分页 meta。</returns>
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