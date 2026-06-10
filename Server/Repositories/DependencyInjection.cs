using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Core.Options;
using SurfWeb.Repositories.Persistence;

namespace SurfWeb.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddSurfWebRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Shavit");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            return services;
        }

        var provider = configuration.GetSection($"{SurfWebOptions.SectionName}:Database:Provider").Get<string>()
            ?? DatabaseOptions.MySql;

        services.AddDbContext<ShavitDbContext>(options =>
        {
            if (DatabaseOptions.IsSqlite(provider))
                options.UseSqlite(connectionString);
            else
            {
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
                options.UseMySql(connectionString, serverVersion);
            }
        });
        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

        return services;
    }
}
