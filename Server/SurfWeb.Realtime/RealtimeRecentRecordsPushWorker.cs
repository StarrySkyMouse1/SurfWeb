using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Options;

namespace SurfWeb.Realtime;

/// <summary>
/// 定时直查数据库，将新完成记录推送到 SignalR 订阅组。
/// </summary>
public sealed class RealtimeRecentRecordsPushWorker(
    IOptions<SurfWebOptions> options,
    RealtimeRecentRecordsPushOrchestrator orchestrator,
    ILogger<RealtimeRecentRecordsPushWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = options.Value.Cache.RecentPushSeconds;
        if (intervalSeconds <= 0)
        {
            logger.LogInformation("实时最新记录推送已禁用（SurfWeb:Cache:RecentPushSeconds = 0）");
            return;
        }

        var interval = TimeSpan.FromSeconds(intervalSeconds);
        logger.LogInformation("实时最新记录推送已启用，轮询间隔 {Seconds}s", intervalSeconds);

        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await orchestrator.RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "实时最新记录轮询或推送失败");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
