using SurfWeb.Application.Web.Middleware;
using Xunit;

namespace SurfWeb.Application.Tests.Web;

public sealed class MinimumResponseDelayMiddlewareTests
{
    [Theory]
    [InlineData(0.2, 200)]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void ResolveDelay_respects_seconds(double seconds, int expectedMs)
    {
        var delay = MinimumResponseDelayMiddleware.ResolveDelay(seconds);
        Assert.Equal(expectedMs, (int)delay.TotalMilliseconds);
    }

    [Theory]
    [InlineData("/api/v1/rankings", true)]
    [InlineData("/api/v1/maps", true)]
    [InlineData("/api/v1/admin/runs", false)]
    [InlineData("/health", false)]
    public void AppliesTo_filters_paths(string path, bool expected)
    {
        Assert.Equal(expected, MinimumResponseDelayMiddleware.AppliesTo(path));
    }
}
