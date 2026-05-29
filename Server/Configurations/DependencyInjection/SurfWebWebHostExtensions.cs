using SurfWeb.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SurfWeb.Configurations.Cors;
using SurfWeb.Configurations.Middleware;

namespace SurfWeb.Configurations;

public static class SurfWebWebHostExtensions
{
    public const string CorsPolicyName = "Web";

    public static WebApplicationBuilder AddSurfWebWebHost(this WebApplicationBuilder builder)
    {
        var surfOptions = builder.Configuration.GetSection(SurfWebOptions.SectionName).Get<SurfWebOptions>()
                          ?? new SurfWebOptions();
        var environment = builder.Environment;

        builder.Services.AddCors(options =>
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

        return builder;
    }

    public static WebApplication UseSurfWebWebHost(this WebApplication app)
    {
        app.UseCors(CorsPolicyName);
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<MinimumResponseDelayMiddleware>();
        return app;
    }
}
