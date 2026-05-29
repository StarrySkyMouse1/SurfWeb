using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SurfWeb.Configurations.Common;
using SurfWeb.Configurations.Cors;
using SurfWeb.Core.Options;

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
            var (status, errorCode, message) = MapException(ex);
            if (!context.Response.HasStarted)
            {
                CorsOriginHelper.ApplyHeaders(context, surfOptions.Value, env);
                context.Response.StatusCode = (int)status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(
                        new { error = new ApiError(errorCode.GetCode(), message) },
                        JsonSerializerOptions.Web));
            }
        }
    }

    private static (HttpStatusCode Status, ApiErrorCode Code, string Message) MapException(Exception ex)
    {
        if (ex is KeyNotFoundException)
            return (HttpStatusCode.NotFound, ApiErrorCode.NotFound, ApiErrorCode.NotFound.GetMessage());

        if (ex is ArgumentException)
            return (HttpStatusCode.BadRequest, ApiErrorCode.BadRequest, ApiErrorCode.BadRequest.GetMessage());

        if (ex.InnerException is MySqlException mysql)
            return MapMySql(mysql);

        if (ex is MySqlException direct)
            return MapMySql(direct);

        return (HttpStatusCode.InternalServerError, ApiErrorCode.ServerError, ApiErrorCode.ServerError.GetMessage());
    }

    private static (HttpStatusCode Status, ApiErrorCode Code, string Message) MapMySql(MySqlException ex) =>
        (HttpStatusCode.ServiceUnavailable, ApiErrorCode.DatabaseUnavailable,
            ex.Message.Contains("connect", StringComparison.OrdinalIgnoreCase)
                ? "无法连接 MySQL：请检查 RDS 白名单、连接串与当前出口 IP 是否已放行"
                : ApiErrorCode.DatabaseUnavailable.GetMessage());
}
