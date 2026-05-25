namespace SurfWeb.Data.Caching;

/// <summary>
/// 查询结果懒刷新缓存：过期后由下一次用户请求触发重新加载（非后台定时任务）。
/// </summary>
public interface IQueryCache
{
    /// <summary>
    /// 按 key 获取缓存；未命中或已过期时执行 factory 并写入缓存。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="ttl">绝对过期时间。</param>
    /// <param name="factory">加载数据的工厂方法。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>缓存或新加载的值。</returns>
    Task<T> GetOrLoadAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default);
}