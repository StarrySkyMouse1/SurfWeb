namespace SurfWeb.Application.Caching;

/// <summary>全量列表缓存条目：最多 SiteLimits 条，分页在内存切片。</summary>
public sealed record CachedPageList<T>(IReadOnlyList<T> Items, int Total);
