using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.ServerStatus.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/servers")]
public sealed class ServersController(IServerStatusService serverStatus) : ControllerBase
{
    /// <summary>
    /// 获取实时服务器状态（Steam A2S + Shavit 补充玩家 auth / 地图 Tier）。
    /// </summary>
    /// <param name="refresh">为 true 时立即触发 Steam 查询刷新。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>服务器状态列表。</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServerStatusDto>>>> List(
        [FromQuery] bool refresh = false,
        CancellationToken ct = default)
    {
        if (refresh)
            await serverStatus.RefreshAsync(ct);

        var list = await serverStatus.GetStatusesAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<ServerStatusDto>>.Ok(list));
    }
}
