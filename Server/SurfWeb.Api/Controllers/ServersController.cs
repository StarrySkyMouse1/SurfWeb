using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Data.Dtos;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/servers")]
public sealed class ServersController(
    IServerService serverQuery,
    IServerStatusRefresher refresher) : ControllerBase
{
    /// <summary>
    /// ??????????Steam A2S + Shavit ?? auth / ?? Tier??
    /// </summary>
    /// <param name="refresh">? true ??????? Steam ???</param>
    /// <param name="ct">?????</param>
    /// <returns>????????</returns>
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