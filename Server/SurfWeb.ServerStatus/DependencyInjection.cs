using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.ServerStatus.IServices;
using SurfWeb.ServerStatus.Services;

namespace SurfWeb.ServerStatus;

public static class DependencyInjection
{
    /// <summary>
    /// 注册服务器在线状态（单例刷新器 + 后台 Host；依赖 Shavit <c>IServices</c> 已注册）。
    /// </summary>
    public static WebApplicationBuilder AddSurfWebServerStatus(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ServerStatusRefresher>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ServerStatusRefresher>());
        builder.Services.AddScoped<IServerStatusService, ServerStatusService>();
        return builder;
    }
}
