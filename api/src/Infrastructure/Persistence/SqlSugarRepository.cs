using System.Linq.Expressions;
using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure.Persistence;

public class SqlSugarRepository<T>(ISqlSugarClient db) : IRepository<T>
    where T : class, new()
{
    protected readonly ISqlSugarClient Db = db;

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await Db.Queryable<T>().InSingleAsync(id);
    }

    public virtual async Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await Db.Queryable<T>().Where(predicate).ToListAsync();
    }

    public virtual async Task<long> AddAsync(T entity, CancellationToken ct = default)
    {
        return await Db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken ct = default)
    {
        return await Db.Updateable(entity).ExecuteCommandAsync() > 0;
    }

    public virtual async Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default)
    {
        return await Db.Updateable<T>()
            .SetColumns("deleted_at", DateTimeOffset.UtcNow)
            .Where("id = @id", new { id })
            .ExecuteCommandAsync() > 0;
    }
}
