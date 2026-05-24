using Microsoft.AspNetCore.Mvc;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Common;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Servers;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/servers")]
public sealed class ServersController(
    IServerQueryService serverQuery,
    ServerStatusRefresher refresher) : ControllerBase
{
    /// <summary>实时服务器状态（Steam A2S + Shavit 玩家 auth / 地图 Tier）。<paramref name="refresh"/> 为 true 时强制刷新缓存。</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServerStatusDto>>>> List(
        [FromQuery] bool refresh = false,
        CancellationToken ct = default)
    {
        if (refresh)
            await refresher.RefreshAsync(ct);

        var list = await serverQuery.GetStatusesAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<ServerStatusDto>>.Ok(list));
    }
}
