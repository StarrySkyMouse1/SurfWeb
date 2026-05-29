using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Utils.Caching;

namespace SurfWeb.Utils;

public static class DependencyInjection
{
    public static IServiceCollection AddSurfWebQueryCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IQueryCache, QueryCache>();
        return services;
    }
}
