namespace SurfWeb.Utils.Caching;

/// <summary>
/// 查询结果懒刷新缓存：过期后由下一次用户请求触发重新加载（非后台定时任务）。
/// </summary>
public interface IQueryCache
{
    /// <summary>
    /// 按 key 获取缓存；未命中或已过期时执行 factory 并写入缓存。
    /// </summary>
    Task<T> GetOrLoadAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default);
}
