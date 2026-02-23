using MovieSite.Domain.Entities;

namespace MovieSite.Domain.Repositories;

public interface ITvSeriesRepository : IRepository<TvSeries>
{
    Task<List<TvSeries>> GetByAirStatusAsync(string airStatus, CancellationToken ct = default);
}
