using MovieSite.Domain.Entities;
using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure.Persistence;

public sealed class TvSeriesRepository(ISqlSugarClient db)
    : SqlSugarRepository<TvSeries>(db), ITvSeriesRepository
{
    public async Task<List<TvSeries>> GetByAirStatusAsync(string airStatus, CancellationToken ct = default)
    {
        return await Db.Queryable<TvSeries>()
            .Where(x => x.AirStatus == airStatus && x.DeletedAt == null)
            .ToListAsync();
    }
}
