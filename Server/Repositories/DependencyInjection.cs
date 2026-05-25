using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Repositories.Persistence;

namespace SurfWeb.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddSurfWebRepositories(
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

        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

        return services;
    }
}
