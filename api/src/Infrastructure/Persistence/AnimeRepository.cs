using MovieSite.Domain.Entities;
using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure.Persistence;

public sealed class AnimeRepository(ISqlSugarClient db)
    : SqlSugarRepository<Anime>(db), IAnimeRepository
{
    public async Task<List<Anime>> GetByOriginAsync(string origin, CancellationToken ct = default)
    {
        return await Db.Queryable<Anime>()
            .Where(x => x.Origin == origin && x.DeletedAt == null)
            .ToListAsync();
    }
}
