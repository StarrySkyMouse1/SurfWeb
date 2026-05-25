using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SurfWeb.Configurations.Cors;
using SurfWeb.Configurations.Middleware;

namespace SurfWeb.Configurations;

public static class SurfWebWebHostExtensions
{
    public const string CorsPolicyName = "Web";

    public static IServiceCollection AddSurfWebWebHost(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var surfOptions = configuration.GetSection(SurfWebOptions.SectionName).Get<SurfWebOptions>()
                          ?? new SurfWebOptions();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(origin =>
                        CorsOriginHelper.IsAllowed(origin, surfOptions, environment));
                }
                else
                {
                    policy.WithOrigins(surfOptions.CorsOrigins);
                }

                policy.AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static WebApplication UseSurfWebWebHost(this WebApplication app)
    {
        app.UseCors(CorsPolicyName);
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<MinimumResponseDelayMiddleware>();
        return app;
    }
}