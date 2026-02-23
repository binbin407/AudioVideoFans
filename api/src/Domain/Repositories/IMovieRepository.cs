using MovieSite.Domain.Entities;

namespace MovieSite.Domain.Repositories;

public interface IMovieRepository : IRepository<Movie>
{
    Task<List<Movie>> GetByFranchiseIdAsync(long franchiseId, CancellationToken ct = default);

    Task<List<Movie>> GetFilteredByGenresAsync(string[] genres, CancellationToken ct = default);
}
