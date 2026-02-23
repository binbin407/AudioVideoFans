using MovieSite.Domain.Entities;

namespace MovieSite.Domain.Repositories;

public interface IAnimeRepository : IRepository<Anime>
{
    Task<List<Anime>> GetByOriginAsync(string origin, CancellationToken ct = default);
}
