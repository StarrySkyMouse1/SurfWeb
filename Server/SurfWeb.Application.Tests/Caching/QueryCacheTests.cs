using Microsoft.Extensions.Caching.Memory;
using SurfWeb.Application.Caching;
using Xunit;

namespace SurfWeb.Application.Tests.Caching;

public sealed class QueryCacheTests
{
    [Fact]
    public async Task GetOrLoadAsync_calls_factory_once_until_expired()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = new QueryCache(memoryCache);
        var factoryCalls = 0;

        async Task<CachedPageList<int>> Factory(CancellationToken _) =>
            new CachedPageList<int>([++factoryCalls], 1);

        var first = await sut.GetOrLoadAsync("test-key", TimeSpan.FromMinutes(10), Factory);
        var second = await sut.GetOrLoadAsync("test-key", TimeSpan.FromMinutes(10), Factory);

        Assert.Equal(1, factoryCalls);
        Assert.Equal(first.Items, second.Items);
    }

    [Fact]
    public async Task GetOrLoadAsync_caches_null_reference_results()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = new QueryCache(memoryCache);
        var factoryCalls = 0;

        Task<string?> Factory(CancellationToken _) =>
            Task.FromResult<string?>(factoryCalls++ == 0 ? null : "unexpected");

        var first = await sut.GetOrLoadAsync("null-key", TimeSpan.FromMinutes(10), Factory);
        var second = await sut.GetOrLoadAsync("null-key", TimeSpan.FromMinutes(10), Factory);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, factoryCalls);
    }
}
