using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurfWeb.Application.Options;

namespace SurfWeb.Application.Servers;

public sealed class ServerStatusRefreshHostedService(
    ServerStatusRefresher refresher,
    IOptions<SurfWebOptions> options,
    ILogger<ServerStatusRefreshHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(5, options.Value.ServerQuery.RefreshSeconds);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await refresher.RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "服务器状态缓存刷新失败");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
