namespace SurfWeb.Application.Caching;

/// <summary>
/// 查询结果懒刷新缓存：过期后由下一次用户请求触发重新加载（非后台定时任务）。
/// </summary>
public interface IQueryCache
{
    Task<T> GetOrLoadAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default);
}
