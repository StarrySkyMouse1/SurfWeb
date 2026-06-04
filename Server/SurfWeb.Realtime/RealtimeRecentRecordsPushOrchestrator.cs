using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfWeb.Core.Dtos;
using SurfWeb.Realtime.Hubs;
using SurfWeb.Services.IServices;

namespace SurfWeb.Realtime;

/// <summary>
/// 最新记录推送轮次：维护 Id 游标，查库并广播（后台 Worker 与开发态手动触发共用）。
/// </summary>
public sealed class RealtimeRecentRecordsPushOrchestrator(
    IServiceScopeFactory scopeFactory,
    IHubContext<RecordsHub> hubContext,
    RealtimeRecordsPushState pushState,
    ILogger<RealtimeRecentRecordsPushOrchestrator> logger)
{
    private int _afterPlayerTimeId;
    private int _afterStageTimeId;
    private bool _cursorInitialized;

    public async Task<RealtimePushCycleResult> RunCycleAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRealtimeRecentRecordsService>();

        var wasInitialized = _cursorInitialized;

        if (!_cursorInitialized)
        {
            (_afterPlayerTimeId, _afterStageTimeId) = await service.GetHighWaterMarksAsync(ct);
            _cursorInitialized = true;
            pushState.SetRevision(DateTimeOffset.UtcNow);
            logger.LogDebug(
                "推送游标已初始化：PlayerTime Id={PlayerTimeId}, StageTime Id={StageTimeId}",
                _afterPlayerTimeId,
                _afterStageTimeId);

            return new RealtimePushCycleResult(
                CursorWasInitialized: wasInitialized,
                CursorInitializedThisCycle: true,
                AfterPlayerTimeId: _afterPlayerTimeId,
                AfterStageTimeId: _afterStageTimeId,
                NewRecordCount: 0,
                Revision: pushState.Revision,
                BroadcastSent: false);
        }

        var poll = await service.PollNewSinceAsync(_afterPlayerTimeId, _afterStageTimeId, ct);
        _afterPlayerTimeId = poll.LastPlayerTimeId;
        _afterStageTimeId = poll.LastStageTimeId;

        if (poll.Items.Count == 0)
        {
            return new RealtimePushCycleResult(
                CursorWasInitialized: true,
                CursorInitializedThisCycle: false,
                AfterPlayerTimeId: _afterPlayerTimeId,
                AfterStageTimeId: _afterStageTimeId,
                NewRecordCount: 0,
                Revision: pushState.Revision,
                BroadcastSent: false);
        }

        pushState.SetRevision(DateTimeOffset.UtcNow);
        await RecordsHub.BroadcastNewRecordsAsync(hubContext, pushState.Revision, poll.Items, ct);

        return new RealtimePushCycleResult(
            CursorWasInitialized: true,
            CursorInitializedThisCycle: false,
            AfterPlayerTimeId: _afterPlayerTimeId,
            AfterStageTimeId: _afterStageTimeId,
            NewRecordCount: poll.Items.Count,
            Revision: pushState.Revision,
            BroadcastSent: true);
    }
}
