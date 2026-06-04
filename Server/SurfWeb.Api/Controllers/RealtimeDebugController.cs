using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Realtime;

namespace SurfWeb.Api.Controllers;

/// <summary>
/// 开发环境：在 Swagger 中手动触发一轮最新记录 SignalR 推送（需已有客户端订阅 Hub）。
/// </summary>
[ApiController]
[Route("api/v1/realtime")]
public sealed class RealtimeDebugController(
    IHostEnvironment environment,
    RealtimeRecentRecordsPushOrchestrator orchestrator) : ControllerBase
{
    /// <summary>
    /// 立即执行一轮推送轮次（查 Id 游标之后的新记录并广播）。首次调用仅初始化游标，不推送历史。
    /// </summary>
    /// <remarks>
    /// 测试步骤：1) 用 SignalR 客户端连接 <c>/hubs/records</c> 并 <c>SubscribeRecent</c>；
    /// 2) 在本接口点 Execute；3) 若库中有新成绩且游标已初始化，订阅端应收到 <c>RecordsUpdated</c>。
    /// 仅 Development 环境可用。
    /// </remarks>
    [HttpPost("push/trigger")]
    [ProducesResponseType(typeof(ApiResponse<RealtimePushCycleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RealtimePushCycleResult>>> TriggerPush(CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var result = await orchestrator.RunCycleAsync(ct);
        return Ok(ApiResponse<RealtimePushCycleResult>.Ok(result));
    }
}
