using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayersController(IPlayerService players) : ControllerBase
{
    /// <summary>
    /// 按 Steam auth 获取玩家资料。
    /// </summary>
    /// <param name="auth">玩家 auth（Steam ID 数值）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>玩家资料。</returns>
    [HttpGet("{auth:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Get(int auth, CancellationToken ct)
    {
        var player = await players.GetPlayerAsync(auth, ct);
        if (player is null) return NotFound(ApiResponse<object>.Fail("not_found", $"Player {auth} not found."));
        return Ok(ApiResponse<object>.Ok(player));
    }

    /// <summary>
    /// 获取玩家成绩列表。
    /// </summary>
    /// <param name="auth">玩家 auth。</param>
    /// <param name="map">可选地图名筛选。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>成绩列表与分页 meta。</returns>
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

    /// <summary>
    /// 获取玩家完成度排行。
    /// </summary>
    /// <param name="auth">玩家 auth。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>完成度列表与分页 meta。</returns>
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