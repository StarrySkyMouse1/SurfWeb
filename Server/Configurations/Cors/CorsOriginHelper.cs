using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace SurfWeb.Configurations.Cors;

public static class CorsOriginHelper
{
    public static bool IsAllowed(string? origin, SurfWebOptions options, IWebHostEnvironment env)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        if (env.IsDevelopment())
            return IsLocalDevHost(uri.Host);

        return options.CorsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    public static void ApplyHeaders(HttpContext context, SurfWebOptions options, IWebHostEnvironment env)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (!IsAllowed(origin, options, env))
            return;

        var headers = context.Response.Headers;
        headers.AccessControlAllowOrigin = origin;
        headers.AccessControlAllowHeaders = "*";
        headers.AccessControlAllowMethods = "GET, HEAD, OPTIONS";
        headers.Vary = "Origin";
    }

    private static bool IsLocalDevHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
}