using System.Linq.Expressions;

namespace MovieSite.Domain.Repositories;

public interface IRepository<T> where T : class, new()
{
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<long> AddAsync(T entity, CancellationToken ct = default);

    Task<bool> UpdateAsync(T entity, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default);
}
