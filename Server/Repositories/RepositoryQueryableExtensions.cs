using Microsoft.EntityFrameworkCore;

namespace SurfWeb.Repositories;

/// <summary>
/// EF 异步查询扩展：避免在 <see cref="IBaseRepository{TEntity}"/> 根上直接调用
/// <see cref="EntityFrameworkQueryableExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/> 时
/// 因自定义 <see cref="IQueryable"/> 实现而触发 IAsyncEnumerable 异常。
/// </summary>
public static class RepositoryQueryableExtensions
{
    public static Task<List<TEntity>> ToListFromDbAsync<TEntity>(
        this IBaseRepository<TEntity> repository,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        if (repository is BaseRepository<TEntity> concrete)
            return concrete.DbSet.AsNoTracking().ToListAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Expected {nameof(BaseRepository<TEntity>)} for EF async queries.");
    }
}
