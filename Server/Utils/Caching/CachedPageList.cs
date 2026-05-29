namespace SurfWeb.Utils.Caching;

/// <summary>
/// 全量列表缓存条目：最多 <see cref="SurfWeb.Core.Constants.SiteLimits"/> 条，分页在内存中切片。
/// </summary>
public sealed record CachedPageList<T>(IReadOnlyList<T> Items, int Total);
