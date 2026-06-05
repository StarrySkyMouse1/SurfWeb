using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SurfWeb.Configurations.Swagger;
using Microsoft.OpenApi.Models;

namespace SurfWeb.Configurations;

public static class SurfWebDefaultExtensions
{
    /// <summary>
    /// ASP.NET Core 默认 API 服务：Controllers（camelCase JSON）、HealthChecks；开发环境含 Swagger。
    /// </summary>
    public static WebApplicationBuilder AddDefault(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var environment = builder.Environment;

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        services.AddHealthChecks();

        if (environment.IsDevelopment())
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "SurfWeb API", Version = "v1" });
                options.ParameterFilter<LatestRecordsTypeParameterFilter>();
            });
        }

        return builder;
    }

    /// <summary>
    /// 开发环境启用 Swagger UI（<c>/swagger</c>）。
    /// </summary>
    public static WebApplication UseDefault(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
