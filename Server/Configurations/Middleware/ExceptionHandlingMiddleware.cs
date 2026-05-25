using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SurfWeb.Configurations.Common;
using SurfWeb.Configurations.Cors;

namespace SurfWeb.Configurations.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IOptions<SurfWebOptions> surfOptions,
    IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var (status, code, message) = MapException(ex);
            if (!context.Response.HasStarted)
            {
                CorsOriginHelper.ApplyHeaders(context, surfOptions.Value, env);
                context.Response.StatusCode = (int)status;
                context.Response.ContentType = "application/json";
                var payload = ApiResponse<object>.Fail(code, message);
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        }
    }

    private static (HttpStatusCode Status, string Code, string Message) MapException(Exception ex)
    {
        if (ex is KeyNotFoundException)
            return (HttpStatusCode.NotFound, "not_found", ex.Message);

        if (ex is ArgumentException)
            return (HttpStatusCode.BadRequest, "bad_request", ex.Message);

        if (ex.InnerException is MySqlException mysql)
            return MapMySql(mysql);

        if (ex is MySqlException direct)
            return MapMySql(direct);

        return (HttpStatusCode.InternalServerError, "server_error", ex.Message);
    }

    private static (HttpStatusCode Status, string Code, string Message) MapMySql(MySqlException ex) =>
        (HttpStatusCode.ServiceUnavailable, "database_unavailable",
            ex.Message.Contains("connect", StringComparison.OrdinalIgnoreCase)
                ? "无法连接 MySQL：请检查 RDS 白名单、连接串与当前出口 IP 是否已放行"
                : ex.Message);
}