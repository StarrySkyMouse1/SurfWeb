using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace SurfWeb.Application.Caching;

public sealed class QueryCache(IMemoryCache memoryCache) : IQueryCache
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<T> GetOrLoadAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default)
    {
        if (memoryCache.TryGetValue(key, out CacheEnvelope<T>? envelope))
            return envelope!.Value;

        var gate = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (memoryCache.TryGetValue(key, out envelope))
                return envelope!.Value;

            var value = await factory(ct).ConfigureAwait(false);
            memoryCache.Set(
                key,
                new CacheEnvelope<T>(value),
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed class CacheEnvelope<T>(T value)
    {
        public T Value { get; } = value;
    }
}
