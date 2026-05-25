using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SurfWeb.Repositories.Persistence;

namespace SurfWeb.Repositories;

/// <summary>
/// 只读通用仓储实现，查询均不跟踪变更。
/// </summary>
public sealed class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class
{
    private readonly ShavitDbContext _dbContext;

    public BaseRepository(ShavitDbContext dbContext) => _dbContext = dbContext;

    public DbSet<TEntity> DbSet => _dbContext.Set<TEntity>();

    public Type ElementType => Query.ElementType;
    public Expression Expression => Query.Expression;
    public IQueryProvider Provider => Query.Provider;

    public IEnumerator<TEntity> GetEnumerator() => Query.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IQueryable<TEntity> Query => DbSet.AsNoTracking();
}
