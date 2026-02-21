---
work_package_id: "WP02"
subtasks:
  - "T007"
  - "T008"
  - "T009"
  - "T010"
  - "T011"
title: ".NET Core 10 DDD Backend Scaffold"
phase: "Phase 0 - Infrastructure Foundation"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP01"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Prompt generated via /spec-kitty.tasks"
---

# Work Package Prompt: WP02 – .NET Core 10 DDD Backend Scaffold

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above. If `has_feedback`, scroll to Review Feedback.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP02 --base WP01
```

---

## Objectives & Success Criteria

- .NET Core 10 solution with 4 project layers: Domain, Application, Infrastructure, API
- All 18+ domain entities defined with correct SqlSugar attributes matching the DB schema
- IRepository interfaces in Domain layer; SqlSugarRepository base in Infrastructure
- SqlSugar DI configured with `PgSqlIsAutoToLower = false`; UnitOfWork working
- Application layer service classes scaffold (no MediatR); base DTOs for all content types
- `dotnet build` succeeds; `dotnet run --project src/API` starts at https://localhost:5001

## Context & Constraints

- **Plan**: `kitty-specs/001-movie-website-platform/plan.md` — DDD 4-layer, SqlSugar, constitution requirements
- **Research**: `kitty-specs/001-movie-website-platform/research.md` — critical SqlSugar patterns (PgSqlIsAutoToLower, TEXT[] raw SQL, Scoped lifetime)
- **Data Model**: `kitty-specs/001-movie-website-platform/data-model.md` — entity column definitions
- Tech: .NET 10, SqlSugar ORM, PostgreSQL 15
- **Constitution mandates**: Controller → Application Service → Repository interface (no skipping layers); Application Layer owns cache invalidation

## Subtasks & Detailed Guidance

### Subtask T007 – Create .NET Solution Structure (4 Layers)

**Purpose**: Scaffold the solution with 4 class library/web API projects following DDD naming conventions.

**Steps**:
1. In `api/` directory:
   ```bash
   dotnet new sln -n MovieSite
   dotnet new classlib -n MovieSite.Domain -f net10.0 -o src/Domain
   dotnet new classlib -n MovieSite.Application -f net10.0 -o src/Application
   dotnet new classlib -n MovieSite.Infrastructure -f net10.0 -o src/Infrastructure
   dotnet new webapi -n MovieSite.API -f net10.0 -o src/API
   dotnet sln add src/Domain src/Application src/Infrastructure src/API
   ```
2. Add project references:
   ```bash
   dotnet add src/Application reference src/Domain
   dotnet add src/Infrastructure reference src/Domain
   dotnet add src/API reference src/Application src/Infrastructure
   ```
3. Install NuGet packages:
   - `src/Domain`: none (pure interfaces/entities)
   - `src/Infrastructure`: `SqlSugarCore`, `Npgsql` (PostgreSQL driver), `StackExchange.Redis`, `COSSTS.NET` (or `TencentCOS.SDK`)
   - `src/API`: `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Sentry.AspNetCore`, `prometheus-net.AspNetCore`
4. Create folder structure inside each project:
   - `Domain/`: `Entities/`, `Repositories/`, `Services/`, `ValueObjects/`
   - `Application/`: `Movies/`, `TvSeries/`, `Anime/`, `People/`, `Search/`, `Rankings/`, `Awards/`, `Admin/`, `Common/` (base DTOs)
   - `Infrastructure/`: `Persistence/` (repositories impl), `Cache/`, `Storage/`
   - `API/`: `Controllers/`, `Middleware/`

**Files**:
- `api/MovieSite.sln`
- `api/src/Domain/MovieSite.Domain.csproj`
- `api/src/Application/MovieSite.Application.csproj`
- `api/src/Infrastructure/MovieSite.Infrastructure.csproj`
- `api/src/API/MovieSite.API.csproj`

**Validation**:
- [ ] `dotnet build api/MovieSite.sln` compiles with 0 errors

---

### Subtask T008 – Define Domain Entities with SqlSugar Attributes

**Purpose**: Create C# entity classes for all 18 tables with correct SqlSugar column mapping attributes.

**Steps**:
1. In `src/Domain/Entities/`, create one file per entity. Key patterns from research.md:
   ```csharp
   [SugarTable("movies")]
   public class Movie
   {
       [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
       public long Id { get; set; }

       [SugarColumn(ColumnName = "title_cn")]
       public string TitleCn { get; set; } = string.Empty;

       // JSONB columns
       [SugarColumn(ColumnName = "release_dates", ColumnDataType = "jsonb", IsJson = true)]
       public List<ReleaseDate> ReleaseDates { get; set; } = new();

       // TEXT[] arrays
       [SugarColumn(ColumnName = "genres", ColumnDataType = "text[]")]
       public string[] Genres { get; set; } = Array.Empty<string>();

       // Generated column (ignore on write)
       [SugarColumn(ColumnName = "search_vector", IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true)]
       public string? SearchVector { get; set; }

       [SugarColumn(ColumnName = "deleted_at")]
       public DateTimeOffset? DeletedAt { get; set; }
   }
   ```
2. Create value object records for JSONB-nested types:
   - `ReleaseDate` (region, date, type string)
   - `Duration` (version, minutes)
   - `DoubanRatingDist` (five, four, three, two, one as float)
   - `NextEpisodeInfo` (air_date, title, season_number, episode_number)
3. Create all entities: Movie, TvSeries, Anime, TvSeason, TvEpisode, AnimeSeason, AnimeEpisode, Person, Credit, Franchise, Keyword, ContentKeyword, MediaVideo, AwardEvent, AwardCeremony, AwardNomination, FeaturedBanner, PendingContent, PageView.

**Files**:
- `api/src/Domain/Entities/Movie.cs`
- `api/src/Domain/Entities/TvSeries.cs`
- `api/src/Domain/Entities/Anime.cs`
- `api/src/Domain/Entities/TvSeason.cs`, `TvEpisode.cs`, `AnimeSeason.cs`, `AnimeEpisode.cs`
- `api/src/Domain/Entities/Person.cs`, `Credit.cs`, `Franchise.cs`
- `api/src/Domain/Entities/Keyword.cs`, `ContentKeyword.cs`, `MediaVideo.cs`
- `api/src/Domain/Entities/AwardEvent.cs`, `AwardCeremony.cs`, `AwardNomination.cs`
- `api/src/Domain/Entities/FeaturedBanner.cs`, `PendingContent.cs`, `PageView.cs`
- `api/src/Domain/ValueObjects/ReleaseDate.cs`, `DoubanRatingDist.cs`, etc.

**Validation**:
- [ ] All entity classes compile; each has `[SugarTable]` and `[SugarColumn]` attributes
- [ ] JSONB classes have matching C# property types for round-trip serialization

---

### Subtask T009 – IRepository Interfaces + SqlSugar Base Implementation

**Purpose**: Define clean repository interfaces in Domain (DDD rule: domain knows nothing about ORM) and implement the generic SqlSugar repository in Infrastructure.

**Steps**:
1. In `src/Domain/Repositories/`, create `IRepository<T>`:
   ```csharp
   public interface IRepository<T> where T : class, new()
   {
       Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
       Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
       Task<long> AddAsync(T entity, CancellationToken ct = default);
       Task<bool> UpdateAsync(T entity, CancellationToken ct = default);
       Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default);
   }
   ```
2. Create specialized interfaces: `IMovieRepository`, `ITvSeriesRepository`, `IAnimeRepository`, `IPersonRepository`, etc. — add any domain-specific query methods (e.g., `IMovieRepository.GetByFranchiseIdAsync(long franchiseId)`).
3. In `src/Infrastructure/Persistence/`, implement `SqlSugarRepository<T>`:
   ```csharp
   public class SqlSugarRepository<T>(ISqlSugarClient db) : IRepository<T> where T : class, new()
   {
       public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
           => await db.Queryable<T>().InSingleAsync(id);

       public async Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default)
           => await db.Updateable<T>()
               .SetColumns("deleted_at", DateTime.UtcNow)
               .Where("id = @id", new { id })
               .ExecuteCommandAsync() > 0;
   }
   ```
4. Implement concrete repositories (e.g., `MovieRepository : SqlSugarRepository<Movie>, IMovieRepository`) with any custom query methods.

**Important**: TEXT[] overlap filter must use raw SQL (SqlSugar cannot translate `&&` operator):
```csharp
// In MovieRepository
public async Task<List<Movie>> GetFilteredAsync(string[] genres, ...)
{
    var sql = "SELECT * FROM movies WHERE genres && @genres::text[] AND deleted_at IS NULL";
    return await _db.Ado.SqlQueryAsync<Movie>(sql, new { genres });
}
```

**Files**:
- `api/src/Domain/Repositories/IRepository.cs`
- `api/src/Domain/Repositories/IMovieRepository.cs` (and others)
- `api/src/Infrastructure/Persistence/SqlSugarRepository.cs`
- `api/src/Infrastructure/Persistence/MovieRepository.cs` (and others)

**Validation**:
- [ ] Domain layer has zero references to SqlSugar namespace
- [ ] Infrastructure implements all Domain interfaces

---

### Subtask T010 – SqlSugar DI Configuration + Unit of Work

**Purpose**: Configure SqlSugar as Scoped in the DI container with PostgreSQL settings, and implement the UnitOfWork pattern for multi-repository transactions.

**Steps**:
1. In `src/API/Program.cs`, add SqlSugar registration:
   ```csharp
   builder.Services.AddScoped<ISqlSugarClient>(sp =>
       new SqlSugarClient(new ConnectionConfig
       {
           DbType = DbType.PostgreSQL,
           ConnectionString = builder.Configuration.GetConnectionString("Default")!,
           IsAutoCloseConnection = true,
           InitKeyType = InitKeyType.Attribute,
           MoreSettings = new ConnMoreSettings
           {
               PgSqlIsAutoToLower = false,  // ⚠️ CRITICAL
               IsAutoRemoveDataCache = true,
           }
       })
   );
   ```
2. Register all repositories:
   ```csharp
   builder.Services.AddScoped<IMovieRepository, MovieRepository>();
   // ... repeat for all repositories
   ```
3. Implement `IUnitOfWork` in Domain:
   ```csharp
   public interface IUnitOfWork
   {
       Task BeginAsync(CancellationToken ct = default);
       Task CommitAsync(CancellationToken ct = default);
       Task RollbackAsync(CancellationToken ct = default);
   }
   ```
4. Implement in Infrastructure using `ISqlSugarClient.BeginTran()`:
   ```csharp
   public class UnitOfWork(ISqlSugarClient db) : IUnitOfWork
   {
       public Task BeginAsync(CancellationToken ct) { db.BeginTran(); return Task.CompletedTask; }
       public Task CommitAsync(CancellationToken ct) { db.CommitTran(); return Task.CompletedTask; }
       public Task RollbackAsync(CancellationToken ct) { db.RollbackTran(); return Task.CompletedTask; }
   }
   ```

**Files**:
- `api/src/API/Program.cs` (update)
- `api/src/Domain/IUnitOfWork.cs`
- `api/src/Infrastructure/UnitOfWork.cs`

**Validation**:
- [ ] `PgSqlIsAutoToLower = false` is present (without this, column names fail)
- [ ] `IUnitOfWork` is registered as Scoped in DI

---

### Subtask T011 – Application Layer Scaffold + Base DTOs

**Purpose**: Establish the Application layer service class structure and create base DTOs shared across all API responses.

**Steps**:
1. In `src/Application/Common/`, create base response DTOs:
   ```csharp
   public record PagedResponse<T>(List<T> Data, PaginationDto Pagination);
   public record PaginationDto(int Page, int PageSize, int Total, int TotalPages);
   public record ApiError(string Code, string Message);
   ```
2. Create `MediaCardDto` (used by all list endpoints):
   ```csharp
   public record MediaCardDto(
       long Id, string ContentType, string TitleCn,
       int? Year, string? PosterCosKey, decimal? DoubanScore, string[] Genres
   );
   ```
3. Create empty service class stubs for each domain area (to be filled in later WPs):
   - `src/Application/Movies/MovieApplicationService.cs`
   - `src/Application/TvSeries/TvSeriesApplicationService.cs`
   - `src/Application/Anime/AnimeApplicationService.cs`
   - `src/Application/People/PeopleApplicationService.cs`
   - `src/Application/Search/SearchApplicationService.cs`
   - `src/Application/Rankings/RankingsApplicationService.cs`
   - `src/Application/Awards/AwardsApplicationService.cs`
   - `src/Application/Admin/AdminApplicationService.cs`
4. Create filter DTO base class:
   ```csharp
   public record ContentListFilter(
       string[]? Genres, string[]? Regions, string? Decade, int? Year,
       string? Language, decimal? MinScore, string Sort = "popularity",
       int Page = 1, int PageSize = 24
   );
   ```

**Files**:
- `api/src/Application/Common/PagedResponse.cs`
- `api/src/Application/Common/MediaCardDto.cs`
- `api/src/Application/Common/ContentListFilter.cs`
- `api/src/Application/Movies/MovieApplicationService.cs` (stub)
- (and other stubs)

**Validation**:
- [ ] `dotnet build` compiles all 4 projects with 0 warnings
- [ ] `dotnet run --project src/API` starts on port 5001; Swagger UI accessible at `/swagger`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| `PgSqlIsAutoToLower = false` forgotten → column name mismatches | Add a startup integration test that does a simple SELECT from `movies` |
| SqlSugar JSONB round-trip serialization errors | Test each JSONB type with an INSERT + SELECT immediately after defining entity |
| TEXT[] mapping might need `NpgsqlDbType.Array` hint | Use `ColumnDataType = "text[]"` attribute; test array filter with real data |
| .NET 10 may have breaking changes from .NET 8 | Use `dotnet new` templates with `-f net10.0`; check breaking change docs |

## Review Guidance

- Confirm `PgSqlIsAutoToLower = false` in Program.cs SqlSugar config
- Verify Domain layer has no `using SqlSugar;` statements (only Interface/Entity definitions)
- Check all 18 entity classes exist with `[SugarTable]` attributes
- Confirm `IUnitOfWork` is registered as Scoped (not Singleton)
- `dotnet build` must produce 0 errors and 0 warnings

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
