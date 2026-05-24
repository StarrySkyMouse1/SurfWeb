using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Commands.RecordRun;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries;
using SurfWeb.Application.Servers;

namespace SurfWeb.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSurfWebOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SurfWebOptions>()
            .Bind(configuration.GetSection(SurfWebOptions.SectionName))
            .PostConfigure(options =>
            {
                var configured = options.Styles.FirstOrDefault(s => s.Default);
                if (configured is not null)
                    options.DefaultStyleId = configured.Id;
            });
        return services;
    }

    public static IServiceCollection AddSurfWebApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IQueryCache, QueryCache>();
        services.AddScoped<IMapQueryService, MapQueryService>();
        services.AddScoped<IPlayerQueryService, PlayerQueryService>();
        services.AddScoped<IRankingQueryService, RankingQueryService>();
        services.AddScoped<IRecordQueryService, RecordQueryService>();
        services.AddScoped<IServerQueryService, ServerQueryService>();
        services.AddScoped<IRecordRunUseCase, RecordRunUseCase>();

        services.AddSingleton<IServerStatusStore, ServerStatusStore>();
        services.AddSingleton<ServerStatusRefresher>();
        services.AddHostedService<ServerStatusRefreshHostedService>();

        return services;
    }
}
