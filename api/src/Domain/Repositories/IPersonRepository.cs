using MovieSite.Domain.Entities;

namespace MovieSite.Domain.Repositories;

public interface IPersonRepository : IRepository<Person>
{
    Task<List<Person>> GetByProfessionAsync(string profession, CancellationToken ct = default);
}
