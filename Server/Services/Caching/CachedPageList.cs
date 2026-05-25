namespace SurfWeb.Data.Caching;

/// <summary>
/// 全量列表缓存条目：最多 SiteLimits 条，分页在内存中切片。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
/// <param name="Items">完整列表快照。</param>
/// <param name="Total">总条数（用于分页 meta）。</param>
public sealed record CachedPageList<T>(IReadOnlyList<T> Items, int Total);