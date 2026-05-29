using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Repositories;
using SurfWeb.Utils;
using SurfWeb.Services.IServices;

namespace SurfWeb.Services;

public static class DependencyInjection
{
    /// <summary>注册 Shavit 只读查询（仓储 + Services + 查询缓存）。</summary>
    public static WebApplicationBuilder AddSurfWeb(this WebApplicationBuilder builder) =>
        builder.AddSurfWebData();

    public static WebApplicationBuilder AddSurfWebData(this WebApplicationBuilder builder)
    {
        builder.Services.AddSurfWebRepositories(builder.Configuration);
        builder.Services.AddSurfWebServices();
        return builder;
    }

    private static void AddSurfWebServices(this IServiceCollection services)
    {
        services.AddSurfWebQueryCache();
        services.AddScoped<IMapService, MapService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IUserService, UserService>();
    }
}
