using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/maps")]
public sealed class MapsController(IMapService maps) : ControllerBase
{
    /// <summary>
    /// 分页获取地图列表。
    /// </summary>
    /// <param name="tier">难度 Tier 筛选。</param>
    /// <param name="search">地图名搜索关键字。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>地图列表与分页 meta。</returns>
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

    /// <summary>
    /// 获取单张地图详情。
    /// </summary>
    /// <param name="mapName">地图名。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>地图详情。</returns>
    [HttpGet("{mapName}")]
    public async Task<ActionResult<ApiResponse<object>>> Detail(
        string mapName,
        CancellationToken ct = default)
    {
        var map = await maps.GetMapAsync(mapName, ct);
        if (map is null) return NotFound(ApiResponse<object>.Fail("not_found", $"Map '{mapName}' not found."));
        return Ok(ApiResponse<object>.Ok(map));
    }

    /// <summary>
    /// 获取地图排行榜。
    /// </summary>
    /// <param name="mapName">地图名。</param>
    /// <param name="track">赛道编号。</param>
    /// <param name="stage">关卡编号；null 表示主关。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>排行榜条目与分页 meta。</returns>
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