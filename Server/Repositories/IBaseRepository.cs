namespace SurfWeb.Repositories;

/// <summary>
/// 只读通用仓储：对实体暴露 <see cref="IQueryable{T}"/>（已 AsNoTracking）。
/// </summary>
public interface IBaseRepository<TEntity> : IQueryable<TEntity>
    where TEntity : class
{
}
