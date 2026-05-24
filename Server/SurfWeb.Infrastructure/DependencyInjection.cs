using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.DomainServices;
using SurfWeb.Domain.Repositories;
using SurfWeb.Infrastructure.Policies;
using SurfWeb.Infrastructure.Persistence;
using SurfWeb.Infrastructure.Repositories.Read;
using SurfWeb.Infrastructure.Repositories.Write;
using SurfWeb.Infrastructure.Steam;

namespace SurfWeb.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSurfWebInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Shavit");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
            services.AddDbContext<ShavitDbContext>(options =>
                options.UseMySql(connectionString, serverVersion));
        }
        else
        {
            services.AddDbContext<ShavitDbContext>(options =>
                options.UseMySql(
                    "Server=localhost;Database=shavit;User=readonly;Password=;",
                    new MySqlServerVersion(new Version(8, 0, 0))));
        }

        services.AddScoped<IMapReadRepository, MapReadRepository>();
        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<IPlayerReadRepository, PlayerReadRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IMapRepository, MapRepository>();
        services.AddScoped<IRunRecordRepository, RunRecordRepository>();
        services.AddScoped<IWorldRecordPolicy, WorldRecordPolicy>();
        services.AddScoped<ICompletionPolicy, CompletionPolicy>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<ISteamServerQuery, SteamServerQueryService>();

        return services;
    }
}
