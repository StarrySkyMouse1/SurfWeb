using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SurfWeb.Configurations;

public static class SurfWebServiceCollectionExtensions
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
}
