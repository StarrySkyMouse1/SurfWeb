using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SurfWeb.Configurations.Middleware;

/// <summary>
/// 读 API 最小响应时间：耗时不足配置值则补齐等待，超过配置值则立即返回。
/// </summary>
public sealed class MinimumResponseDelayMiddleware(
    RequestDelegate next,
    IOptions<SurfWebOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var delay = ResolveDelay(options.Value.MinResponseDelaySeconds);
        if (delay <= TimeSpan.Zero || !AppliesTo(context.Request.Path))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        await next(context);
        var remaining = delay - Stopwatch.GetElapsedTime(started);
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, context.RequestAborted);
    }

    public static TimeSpan ResolveDelay(double seconds) =>
        seconds <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(seconds);

    public static bool AppliesTo(PathString path)
    {
        if (!path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase))
            return false;
        return !path.StartsWithSegments("/api/v1/admin", StringComparison.OrdinalIgnoreCase);
    }
}