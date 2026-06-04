using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayersController(IPlayerService players) : ControllerBase
{
    /// <summary>
    /// 按 Steam auth 获取玩家冲浪档案摘要（积分 / 时长 / 完成 / WR 及排名）。
    /// </summary>
    [HttpGet("{auth:int}")]
    public async Task<ActionResult<ApiResponse<PlayerSummaryDto>>> Get(int auth, CancellationToken ct)
    {
        var player = await players.GetPlayerAsync(auth, ct);
        if (player is null)
            return NotFound(ApiResponse<PlayerSummaryDto>.Fail(ApiErrorCode.NotFound, $"未找到玩家 {auth}。"));
        return Ok(ApiResponse<PlayerSummaryDto>.Ok(player));
    }

    /// <summary>
    /// 玩家记录列表与图表（近期 / WR / 未完成 × 主线 / 阶段 / 奖励）。
    /// </summary>
    [HttpGet("{auth:int}/records")]
    public async Task<ActionResult<ApiResponse<PlayerRecordsPageDto>>> Records(
        int auth,
        [FromQuery] PlayerRecordCategory category = PlayerRecordCategory.Recent,
        [FromQuery] PlayerRecordScope scope = PlayerRecordScope.Main,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? tier = null,
        CancellationToken ct = default)
    {
        if (tier is < 0 or > 8)
            return BadRequest(ApiResponse<PlayerRecordsPageDto>.Fail(ApiErrorCode.BadRequest, "tier 须为 0–8。"));

        var result = await players.GetPlayerRecordsAsync(auth, category, scope, page, pageSize, tier, ct);
        if (result is null)
            return NotFound(ApiResponse<PlayerRecordsPageDto>.Fail(ApiErrorCode.NotFound, $"未找到玩家 {auth}。"));

        var (pageDto, total) = result.Value;
        return Ok(ApiResponse<PlayerRecordsPageDto>.Ok(pageDto, new ApiMeta(page, pageSize, total)));
    }
}
