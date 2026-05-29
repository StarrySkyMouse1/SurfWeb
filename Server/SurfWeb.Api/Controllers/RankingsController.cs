using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Services.IServices;
using SurfWeb.Core.Enums;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/rankings")]
public sealed class RankingsController(IRankingService rankings) : ControllerBase
{
    /// <summary>
    /// 获取全站排行榜。
    /// </summary>
    /// <param name="type">排行类型：points / playtime / completions / wr。</param>
    /// <param name="wrScope">仅 type=wr 时有效：main / bonus / stage。</param>
    /// <param name="completionScope">仅 type=completions 时有效：main / bonus。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>排行榜与分页 meta。</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RankingEntryDto>>>> List(
        [FromQuery] RankingType type = RankingType.Points,
        [FromQuery] WrRankingScope wrScope = WrRankingScope.Main,
        [FromQuery] TrackRankingScope completionScope = TrackRankingScope.Main,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await rankings.GetRankingsAsync(
            type, page, pageSize, wrScope, completionScope, ct);
        return Ok(ApiResponse<IReadOnlyList<RankingEntryDto>>.Ok(items, new ApiMeta(page, pageSize, total)));
    }
}
