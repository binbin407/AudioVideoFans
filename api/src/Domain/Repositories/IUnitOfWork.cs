namespace MovieSite.Domain.Repositories;

public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
