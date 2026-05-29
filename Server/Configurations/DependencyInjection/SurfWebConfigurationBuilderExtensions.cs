using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace SurfWeb.Configurations;

public static class SurfWebConfigurationBuilderExtensions
{
    /// <summary>
    /// 加载可选本地配置：<c>appsettings.local.json</c>、<c>appsettings.{Environment}.local.json</c>。
    /// </summary>
    public static WebApplicationBuilder AddSurfWebLocalConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{builder.Environment.EnvironmentName}.local.json",
                optional: true,
                reloadOnChange: true);

        return builder;
    }
}
