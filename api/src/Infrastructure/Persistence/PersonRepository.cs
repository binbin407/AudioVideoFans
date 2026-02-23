using MovieSite.Domain.Entities;
using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure.Persistence;

public sealed class PersonRepository(ISqlSugarClient db)
    : SqlSugarRepository<Person>(db), IPersonRepository
{
    public async Task<List<Person>> GetByProfessionAsync(string profession, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM people WHERE professions @> ARRAY[@profession]::text[] AND deleted_at IS NULL";
        return await Db.Ado.SqlQueryAsync<Person>(sql, new { profession });
    }
}
