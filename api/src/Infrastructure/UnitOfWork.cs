using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure;

public sealed class UnitOfWork(ISqlSugarClient db) : IUnitOfWork
{
    public Task BeginAsync(CancellationToken ct = default)
    {
        db.Ado.BeginTran();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        db.Ado.CommitTran();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        db.Ado.RollbackTran();
        return Task.CompletedTask;
    }
}
