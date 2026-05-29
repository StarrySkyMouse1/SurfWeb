using Microsoft.AspNetCore.Builder;

namespace SurfWeb.Configurations;

public static class SurfWebApplicationBuilderExtensions
{
    /// <summary>
    /// 注册 API 宿主层：本地配置、默认框架服务、<see cref="SurfWebOptions"/>、CORS 与中间件依赖。
    /// </summary>
    public static WebApplicationBuilder AddSurfWebApi(this WebApplicationBuilder builder)
    {
        builder.AddSurfWebLocalConfiguration();
        builder.AddDefault();
        builder.AddSurfWebOptions();
        builder.AddSurfWebWebHost();
        return builder;
    }
}
