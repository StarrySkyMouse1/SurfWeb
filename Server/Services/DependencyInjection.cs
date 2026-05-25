using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Data.Caching;
using SurfWeb.Data.Servers;
using SurfWeb.Data.Steam;
using SurfWeb.Repositories;
using SurfWeb.Services.IServices;

namespace SurfWeb.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddSurfWeb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSurfWebRepositories(configuration);
        services.AddSurfWebServices();
        services.AddSurfWebInfrastructure();
        return services;
    }

    public static IServiceCollection AddSurfWebServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IQueryCache, QueryCache>();
        services.AddScoped<IMapService, MapService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IServerService, ServerService>();

        return services;
    }

    public static IServiceCollection AddSurfWebInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISteamServerQuery, SteamServerQueryService>();
        services.AddSingleton<IServerStatusStore, ServerStatusStore>();
        services.AddSingleton<ServerStatusRefresher>();
        services.AddSingleton<IServerStatusRefresher>(sp => sp.GetRequiredService<ServerStatusRefresher>());
        services.AddHostedService<ServerStatusRefreshHostedService>();

        return services;
    }
}
