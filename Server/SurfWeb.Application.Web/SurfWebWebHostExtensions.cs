using Microsoft.AspNetCore.Builder;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using SurfWeb.Application.Options;

using SurfWeb.Application.Web.Cors;

using SurfWeb.Application.Web.Middleware;



namespace SurfWeb.Application.Web;



public static class SurfWebWebHostExtensions

{

    public const string CorsPolicyName = "Web";



    /// <summary>注册 CORS 策略（策略名 <see cref="CorsPolicyName"/>）。需已调用 <c>AddSurfWebOptions</c> 或自行绑定 <see cref="SurfWebOptions"/>。</summary>

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



    /// <summary>管道顺序：CORS → 全局异常 → 最小响应延迟。须在 <c>MapControllers</c> 之前调用。</summary>

    public static WebApplication UseSurfWebWebHost(this WebApplication app)

    {

        app.UseCors(CorsPolicyName);

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseMiddleware<MinimumResponseDelayMiddleware>();

        return app;

    }

}

