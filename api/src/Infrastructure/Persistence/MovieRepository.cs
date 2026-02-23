using MovieSite.Domain.Entities;
using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure.Persistence;

public sealed class MovieRepository(ISqlSugarClient db)
    : SqlSugarRepository<Movie>(db), IMovieRepository
{
    public async Task<List<Movie>> GetByFranchiseIdAsync(long franchiseId, CancellationToken ct = default)
    {
        return await Db.Queryable<Movie>()
            .Where(x => x.FranchiseId == franchiseId && x.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<List<Movie>> GetFilteredByGenresAsync(string[] genres, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM movies WHERE genres && @genres::text[] AND deleted_at IS NULL";
        return await Db.Ado.SqlQueryAsync<Movie>(sql, new { genres });
    }
}
