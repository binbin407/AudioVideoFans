# SqlSugar + PostgreSQL + DDD Research

**Date**: 2026-02-21
**Scope**: .NET Core 10 Web API, DDD (Domain / Application / Infrastructure / API layers), SqlSugar ORM, PostgreSQL 15

---

## 1. IRepository Pattern with SqlSugar

### 1.1 Generic Repository Interface (Domain Layer)

Place this in `Domain/Interfaces/` — keep it ORM-agnostic; the domain layer must not reference SqlSugar.

```csharp
// Domain/Interfaces/IRepository.cs
public interface IRepository<TEntity, TId>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TId id, CancellationToken ct = default);

    // Soft-delete support (deleted_at pattern used throughout this project)
    Task SoftDeleteAsync(TId id, CancellationToken ct = default);
}
```

For entities that need richer querying, add a second interface rather than polluting the base:

```csharp
// Domain/Interfaces/IMovieRepository.cs
public interface IMovieRepository : IRepository<Movie, long>
{
    Task<(IReadOnlyList<Movie> Items, int Total)> PagedQueryAsync(
        MovieFilter filter, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<Movie>> GetByFranchiseAsync(long franchiseId, CancellationToken ct = default);
    Task<IReadOnlyList<Movie>> GetSimilarAsync(long movieId, int count, CancellationToken ct = default);
}
```

### 1.2 Abstract Base Repository (Infrastructure Layer)

SqlSugar's `ISqlSugarClient` is the entry point. Inject it via the constructor.

```csharp
// Infrastructure/Repositories/BaseRepository.cs
public abstract class BaseRepository<TEntity, TId>
    : IRepository<TEntity, TId>
    where TEntity : class, new()
{
    protected readonly ISqlSugarClient Db;

    protected BaseRepository(ISqlSugarClient db) => Db = db;

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await Db.Queryable<TEntity>()
                   .Where(It.IsAny<TEntity>()) // placeholder; override per entity
                   .InSingleAsync(id);         // SqlSugar: query by primary key

    // Concrete pattern — use the primary key column name via SugarColumn attribute
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct)
        => await Db.Queryable<TEntity>().In(id).SingleAsync();

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => (await Db.Queryable<TEntity>().ToListAsync()).AsReadOnly();

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await Db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
        => await Db.Updateable(entity).ExecuteCommandAsync();

    public virtual async Task DeleteAsync(TId id, CancellationToken ct = default)
        => await Db.Deleteable<TEntity>().In(id).ExecuteCommandAsync();

    public virtual async Task SoftDeleteAsync(TId id, CancellationToken ct = default)
        => await Db.Updateable<TEntity>()
                   .SetColumns("deleted_at", DateTime.UtcNow)
                   .Where("id = @id", new { id })
                   .ExecuteCommandAsync();
}
```

### 1.3 Concrete Repository Example (Movie)

```csharp
// Infrastructure/Repositories/MovieRepository.cs
public sealed class MovieRepository
    : BaseRepository<Movie, long>, IMovieRepository
{
    public MovieRepository(ISqlSugarClient db) : base(db) { }

    // Override to apply soft-delete filter globally
    public override async Task<Movie?> GetByIdAsync(long id, CancellationToken ct = default)
        => await Db.Queryable<Movie>()
                   .Where(m => m.Id == id && m.DeletedAt == null)
                   .SingleAsync();

    public async Task<(IReadOnlyList<Movie> Items, int Total)> PagedQueryAsync(
        MovieFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Db.Queryable<Movie>()
                      .Where(m => m.DeletedAt == null && m.Status == ContentStatus.Published);

        if (filter.Genres?.Length > 0)
            // PostgreSQL array overlap operator — see Section 3
            query = query.Where(
                $"genres && ARRAY[{string.Join(",", filter.Genres.Select(g => $"'{g}'"))}]::text[]");

        if (filter.MinScore.HasValue)
            query = query.Where(m => m.DoubanScore >= filter.MinScore.Value);

        if (filter.Decade.HasValue)
        {
            int from = filter.Decade.Value;
            query = query.Where(m => m.ReleaseYear >= from && m.ReleaseYear < from + 10);
        }

        query = filter.SortBy switch
        {
            MovieSortBy.Score      => query.OrderByDescending(m => m.DoubanScore),
            MovieSortBy.Popularity => query.OrderByDescending(m => m.Popularity),
            _                      => query.OrderByDescending(m => m.Id)
        };

        var (total, items) = await query
            .ToPageListAsync(page, pageSize);

        return (items.AsReadOnly(), total);
    }

    public async Task<IReadOnlyList<Movie>> GetByFranchiseAsync(
        long franchiseId, CancellationToken ct = default)
        => (await Db.Queryable<Movie>()
                    .Where(m => m.FranchiseId == franchiseId && m.DeletedAt == null)
                    .OrderBy(m => m.ReleaseYear)
                    .ToListAsync()).AsReadOnly();

    public async Task<IReadOnlyList<Movie>> GetSimilarAsync(
        long movieId, int count, CancellationToken ct = default)
    {
        // Join via ContentKeyword then fall back to genre overlap
        var sql = @"
            SELECT m.*
            FROM movies m
            INNER JOIN content_keywords ck ON ck.content_id = m.id AND ck.content_type = 'movie'
            WHERE ck.keyword_id IN (
                SELECT keyword_id FROM content_keywords
                WHERE content_id = @movieId AND content_type = 'movie'
            )
            AND m.id <> @movieId
            AND m.deleted_at IS NULL
            AND m.status = 'published'
            GROUP BY m.id
            ORDER BY COUNT(*) DESC
            LIMIT @count";

        return (await Db.Ado.SqlQueryAsync<Movie>(sql, new { movieId, count })).AsReadOnly();
    }
}
```

### 1.4 Entity Mapping with SugarColumn Attributes

```csharp
// Domain/Entities/Movie.cs
[SugarTable("movies")]
public class Movie
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "title_cn")]
    public string TitleCn { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "title_original")]
    public string? TitleOriginal { get; set; }

    [SugarColumn(ColumnName = "douban_score")]
    public decimal? DoubanScore { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "published";

    [SugarColumn(ColumnName = "deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [SugarColumn(ColumnName = "franchise_id")]
    public long? FranchiseId { get; set; }

    [SugarColumn(ColumnName = "popularity")]
    public int Popularity { get; set; }

    [SugarColumn(ColumnName = "release_year")]
    public int ReleaseYear { get; set; }

    // JSONB and array columns — see Sections 2 and 3
    [SugarColumn(ColumnName = "release_dates", ColumnDataType = "jsonb",
                 IsJson = true)]
    public List<ReleaseDate> ReleaseDates { get; set; } = new();

    [SugarColumn(ColumnName = "douban_rating_dist", ColumnDataType = "jsonb",
                 IsJson = true)]
    public DoubanRatingDistribution? DoubanRatingDist { get; set; }

    [SugarColumn(ColumnName = "genres", ColumnDataType = "text[]")]
    public string[] Genres { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "title_aliases", ColumnDataType = "text[]")]
    public string[] TitleAliases { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "extra_backdrops", ColumnDataType = "text[]")]
    public string[] ExtraBackdrops { get; set; } = Array.Empty<string>();
}
```

---

## 2. JSONB Columns in PostgreSQL with SqlSugar

### 2.1 Core Approach

SqlSugar supports JSONB columns through the `IsJson = true` flag on `SugarColumn`. SqlSugar serializes/deserializes the C# object to JSON when reading and writing. You must also set `ColumnDataType = "jsonb"` to ensure schema generation uses the correct PostgreSQL type.

```csharp
[SugarColumn(ColumnName = "release_dates", ColumnDataType = "jsonb", IsJson = true)]
public List<ReleaseDate> ReleaseDates { get; set; } = new();
```

### 2.2 POCO Types for JSONB Columns

These are plain C# types — no SqlSugar attributes needed, just JSON-serializable properties.

```csharp
// Domain/ValueObjects/ReleaseDate.cs
public record ReleaseDate
{
    [JsonPropertyName("region")]
    public string Region { get; init; } = string.Empty;   // "CN", "US", "UK" …

    [JsonPropertyName("date")]
    public DateOnly Date { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    // premiere | limited | theatrical | digital | physical | tv
}

// Domain/ValueObjects/DoubanRatingDistribution.cs
public record DoubanRatingDistribution
{
    [JsonPropertyName("five")]    public decimal Five   { get; init; }  // 力荐 %
    [JsonPropertyName("four")]    public decimal Four   { get; init; }  // 推荐 %
    [JsonPropertyName("three")]   public decimal Three  { get; init; }  // 还行 %
    [JsonPropertyName("two")]     public decimal Two    { get; init; }  // 较差 %
    [JsonPropertyName("one")]     public decimal One    { get; init; }  // 很差 %
}

// Domain/ValueObjects/NextEpisodeInfo.cs (TVSeries / Anime)
public record NextEpisodeInfo
{
    [JsonPropertyName("episode_number")]  public int EpisodeNumber  { get; init; }
    [JsonPropertyName("name")]            public string? Name       { get; init; }
    [JsonPropertyName("air_date")]        public DateOnly? AirDate  { get; init; }
}

// Domain/ValueObjects/FamilyMember.cs (Person)
public record FamilyMember
{
    [JsonPropertyName("name")]      public string Name     { get; init; } = string.Empty;
    [JsonPropertyName("relation")] public string Relation { get; init; } = string.Empty;
}
```

### 2.3 Configuring the JSON Serializer

SqlSugar uses `System.Text.Json` internally for JSONB serialization. Configure this globally when registering SqlSugar:

```csharp
// Infrastructure/Configuration/SqlSugarSetup.cs
StaticConfig.CustomSerializer = new SystemTextJsonSerializer(new JsonSerializerOptions
{
    PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    Converters                  = { new DateOnlyJsonConverter() }
});
```

`DateOnly` is not natively supported by `System.Text.Json` prior to .NET 7. In .NET 10 it is — no custom converter needed.

### 2.4 Querying Inside JSONB Columns

For direct JSONB path queries, use raw SQL predicates via `Where(rawSql)`:

```csharp
// Find movies with a CN theatrical release after 2023-01-01
var movies = await Db.Queryable<Movie>()
    .Where("release_dates @> '[{\"region\":\"CN\",\"type\":\"theatrical\"}]'::jsonb")
    .Where(m => m.DeletedAt == null)
    .ToListAsync();
```

For more complex JSONB operations use `Db.Ado.SqlQueryAsync<T>` with raw SQL — JSONB expressions do not map well to LINQ.

### 2.5 PendingContent raw_data JSONB

```csharp
[SugarTable("pending_content")]
public class PendingContent
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "raw_data", ColumnDataType = "jsonb", IsJson = true)]
    public JsonDocument RawData { get; set; } = JsonDocument.Parse("{}");
    // Use JsonDocument when the schema is unknown at compile-time.
    // For known schemas use a concrete POCO with IsJson = true.

    [SugarColumn(ColumnName = "review_status")]
    public string ReviewStatus { get; set; } = "pending";

    [SugarColumn(ColumnName = "content_type")]
    public string ContentType { get; set; } = string.Empty;
}
```

If `raw_data` structure varies by `content_type`, keep it as `JsonDocument` and deserialize in the application service:

```csharp
// Application/Services/PendingContentService.cs
public async Task<MovieCreateCommand> PreFillMovieFromRawDataAsync(long pendingId)
{
    var pending = await _pendingRepo.GetByIdAsync(pendingId)
        ?? throw new NotFoundException(pendingId);

    using var doc = pending.RawData;
    var root = doc.RootElement;

    return new MovieCreateCommand
    {
        TitleCn      = root.GetProperty("title_cn").GetString() ?? "",
        Synopsis     = root.TryGetProperty("synopsis", out var s) ? s.GetString() : null,
        DoubanScore  = root.TryGetProperty("douban_score", out var ds)
                       ? ds.GetDecimal() : null,
    };
}
```

---

## 3. Array Columns (TEXT[]) in PostgreSQL with SqlSugar

### 3.1 Mapping

Declare the property as `string[]` and set `ColumnDataType = "text[]"`:

```csharp
[SugarColumn(ColumnName = "genres", ColumnDataType = "text[]")]
public string[] Genres { get; set; } = Array.Empty<string>();

[SugarColumn(ColumnName = "professions", ColumnDataType = "text[]")]
public string[] Professions { get; set; } = Array.Empty<string>();

[SugarColumn(ColumnName = "photos_cos_keys", ColumnDataType = "text[]")]
public string[] PhotosCosKeys { get; set; } = Array.Empty<string>();
```

SqlSugar maps `string[]` to `text[]` via Npgsql's native array support automatically when `ColumnDataType` is set.

### 3.2 Querying Array Columns

SqlSugar LINQ cannot translate PostgreSQL array operators (`&&`, `@>`, `ANY`). Use raw SQL predicates inside `.Where()` or fall back to `Db.Ado`:

```csharp
// Contains ALL of these genres (array containment @>)
var sciFiAction = await Db.Queryable<Movie>()
    .Where("genres @> ARRAY['sci-fi','action']::text[]")
    .Where(m => m.DeletedAt == null)
    .ToListAsync();

// Overlaps with ANY of these genres (array overlap &&)
var anyGenre = await Db.Queryable<Movie>()
    .Where($"genres && ARRAY[{string.Join(",", genres.Select(g => $"'{g}'"))}]::text[]")
    .ToListAsync();

// ANY scalar element check
var hasDirector = await Db.Queryable<Person>()
    .Where("'director' = ANY(professions)")
    .ToListAsync();
```

**IMPORTANT — SQL injection**: never interpolate user input directly into raw SQL strings. Parameterize:

```csharp
// Safe parameterized array overlap using Npgsql parameter
var safeQuery = await Db.Ado.SqlQueryAsync<Movie>(
    "SELECT * FROM movies WHERE genres && @genres::text[] AND deleted_at IS NULL",
    new { genres = filter.Genres });
```

### 3.3 Index Recommendation

Add a GIN index for array columns that are frequently filtered:

```sql
CREATE INDEX idx_movies_genres ON movies USING GIN (genres);
CREATE INDEX idx_person_professions ON people USING GIN (professions);
```

---

## 4. Unit of Work Pattern with SqlSugar in DDD

### 4.1 SqlSugar's Built-in Transaction Support

SqlSugar's `ISqlSugarClient` has built-in transaction management. The standard DDD Unit of Work wraps a database transaction, commits on success, and rolls back on failure.

```csharp
// Domain/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

```csharp
// Infrastructure/Persistence/UnitOfWork.cs
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ISqlSugarClient _db;
    private bool _disposed;

    public UnitOfWork(ISqlSugarClient db) => _db = db;

    public Task BeginAsync(CancellationToken ct = default)
    {
        _db.BeginTran();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        _db.CommitTran();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        _db.RollbackTran();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // SqlSugarClient connection management is handled internally
            _disposed = true;
        }
    }
}
```

### 4.2 Application Service Using Unit of Work

The application layer orchestrates domain logic and owns transaction boundaries. Never let the infrastructure layer decide when to commit.

```csharp
// Application/Services/ContentApprovalService.cs
public sealed class ContentApprovalService
{
    private readonly IUnitOfWork        _uow;
    private readonly IPendingContentRepository _pendingRepo;
    private readonly IMovieRepository   _movieRepo;

    public ContentApprovalService(
        IUnitOfWork uow,
        IPendingContentRepository pendingRepo,
        IMovieRepository movieRepo)
    {
        _uow         = uow;
        _pendingRepo = pendingRepo;
        _movieRepo   = movieRepo;
    }

    public async Task ApproveAsMovieAsync(
        long pendingId, MovieCreateCommand cmd, CancellationToken ct = default)
    {
        await _uow.BeginAsync(ct);
        try
        {
            var pending = await _pendingRepo.GetByIdAsync(pendingId, ct)
                ?? throw new NotFoundException($"PendingContent {pendingId} not found");

            var movie = Movie.Create(cmd);   // domain factory method
            await _movieRepo.AddAsync(movie, ct);

            pending.Approve(movie.Id);       // domain method sets status + reviewed_at
            await _pendingRepo.UpdateAsync(pending, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
```

### 4.3 Scoped Lifetime — Critical Rule

`ISqlSugarClient` must be registered as **Scoped** (one instance per HTTP request). All repositories and the Unit of Work that are injected in the same request share the same `ISqlSugarClient` instance — this is how the transaction wraps multiple repository operations automatically.

If you register `ISqlSugarClient` as Singleton or Transient, transactions across repositories will not work correctly.

### 4.4 Avoiding the "Multiple DbContext" Anti-pattern

SqlSugar uses a single connection pool under the hood. Unlike EF Core, you do not need to explicitly share a `DbContext` — sharing the `ISqlSugarClient` scope achieves the same effect. Do not create multiple `SqlSugarClient` instances per request.

---

## 5. Configuring SqlSugar with PostgreSQL in .NET Core DI

### 5.1 NuGet Packages

```xml
<!-- Backend.Infrastructure.csproj -->
<PackageReference Include="SqlSugarCore"   Version="5.1.*" />
<PackageReference Include="Npgsql"         Version="8.*"   />
```

SqlSugar ships `DbType.PostgreSQL` support natively; it uses Npgsql under the hood.

### 5.2 appsettings Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=audiovisual_fans;Username=app_user;Password=CHANGEME;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;"
  },
  "SqlSugar": {
    "EnableSqlLog": false,
    "SlowQueryThresholdMs": 500
  }
}
```

```json
// appsettings.Development.json
{
  "SqlSugar": {
    "EnableSqlLog": true,
    "SlowQueryThresholdMs": 100
  }
}
```

### 5.3 DI Registration

```csharp
// Infrastructure/Extensions/SqlSugarServiceExtensions.cs
public static class SqlSugarServiceExtensions
{
    public static IServiceCollection AddSqlSugar(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connStr     = configuration.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("DefaultConnection is missing");
        var enableLog   = configuration.GetValue<bool>("SqlSugar:EnableSqlLog");
        var slowMs      = configuration.GetValue<int>("SqlSugar:SlowQueryThresholdMs", 500);

        // Register as Scoped — one instance per HTTP request
        services.AddScoped<ISqlSugarClient>(_ =>
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                DbType                = DbType.PostgreSQL,
                ConnectionString      = connStr,
                IsAutoCloseConnection = true,    // returns connection to pool after each query
                InitKeyType           = InitKeyType.Attribute, // use [SugarColumn] attributes
                MoreSettings          = new ConnMoreSettings
                {
                    PgSqlIsAutoToLower           = false,  // preserve C# property casing
                    IsAutoRemoveDataCache        = true,   // auto-clear query cache on write
                }
            });

            // SQL logging
            if (enableLog)
            {
                db.Aop.OnLogExecuting = (sql, pars) =>
                {
                    // Replace with ILogger in production; Debug.WriteLine for dev
                    Debug.WriteLine($"[SQL] {sql}");
                    Debug.WriteLine($"[PARAMS] {db.Utilities.SerializeObject(pars)}");
                };
            }

            // Slow query warning
            db.Aop.OnLogExecuted = (sql, pars) =>
            {
                if (db.Ado.SqlExecutionTime.TotalMilliseconds > slowMs)
                {
                    Debug.WriteLine(
                        $"[SLOW QUERY {db.Ado.SqlExecutionTime.TotalMilliseconds}ms] {sql}");
                }
            };

            // Global exception handler
            db.Aop.OnError = ex =>
            {
                Debug.WriteLine($"[SqlSugar Error] {ex.Sql}\n{ex.ParametersString}");
            };

            return db;
        });

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IMovieRepository,        MovieRepository>();
        services.AddScoped<IPersonRepository,       PersonRepository>();
        services.AddScoped<ITVSeriesRepository,     TVSeriesRepository>();
        services.AddScoped<IAnimeRepository,        AnimeRepository>();
        services.AddScoped<IPendingContentRepository, PendingContentRepository>();
        // … register remaining repositories

        return services;
    }
}
```

### 5.4 Program.cs Wiring

```csharp
// API/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlSugar(builder.Configuration);
builder.Services.AddControllers();
// … other services

var app = builder.Build();
app.MapControllers();
app.Run();
```

### 5.5 Using ISqlSugarClient with ILogger (Production)

In production, replace `Debug.WriteLine` with structured logging:

```csharp
services.AddScoped<ISqlSugarClient>(sp =>
{
    var logger  = sp.GetRequiredService<ILogger<ISqlSugarClient>>();
    var connStr = configuration.GetConnectionString("DefaultConnection")!;

    var db = new SqlSugarClient(new ConnectionConfig
    {
        DbType                = DbType.PostgreSQL,
        ConnectionString      = connStr,
        IsAutoCloseConnection = true,
        InitKeyType           = InitKeyType.Attribute,
    });

    db.Aop.OnLogExecuting = (sql, pars) =>
        logger.LogDebug("SQL: {Sql} | Params: {Params}",
            sql, db.Utilities.SerializeObject(pars));

    db.Aop.OnError = ex =>
        logger.LogError(ex, "SqlSugar error — SQL: {Sql}", ex.Sql);

    return db;
});
```

---

## 6. Project Layer Mapping Summary

```
AudioVideoFans.Domain/
├── Entities/
│   ├── Movie.cs              [SugarTable, SugarColumn attrs; IsJson, text[] columns]
│   ├── TVSeries.cs
│   ├── Anime.cs
│   ├── Person.cs
│   ├── PendingContent.cs
│   └── …
├── ValueObjects/
│   ├── ReleaseDate.cs        [JSONB POCO]
│   ├── DoubanRatingDistribution.cs
│   ├── NextEpisodeInfo.cs
│   └── FamilyMember.cs
└── Interfaces/
    ├── IRepository.cs
    ├── IUnitOfWork.cs
    ├── IMovieRepository.cs
    └── …

AudioVideoFans.Infrastructure/
├── Persistence/
│   └── UnitOfWork.cs
├── Repositories/
│   ├── BaseRepository.cs
│   ├── MovieRepository.cs
│   └── …
└── Extensions/
    └── SqlSugarServiceExtensions.cs

AudioVideoFans.Application/
└── Services/
    ├── ContentApprovalService.cs   [owns transaction boundaries via IUnitOfWork]
    └── …

AudioVideoFans.API/
└── Program.cs                      [builder.Services.AddSqlSugar(config)]
```

---

## 7. Key Gotchas and Production Notes

| Topic | Gotcha | Resolution |
|-------|--------|------------|
| Scoped lifetime | Singleton `ISqlSugarClient` breaks transactions across repositories | Always register as `Scoped` |
| `PgSqlIsAutoToLower` | SqlSugar defaults to lowercasing all column names for PostgreSQL, which conflicts with snake_case attributes | Set `PgSqlIsAutoToLower = false` and rely on explicit `ColumnName` in `[SugarColumn]` |
| `text[]` arrays | LINQ `.Where()` cannot translate `&&` / `ANY` / `@>` | Use raw SQL string in `.Where(rawSql)` or `Db.Ado.SqlQueryAsync`; parameterize to avoid injection |
| JSONB queries | SqlSugar cannot translate `@>` or `->>` operators | Use `Db.Ado.SqlQueryAsync` for complex JSONB predicates |
| `IsAutoCloseConnection = true` | Connection is returned to pool after each statement; no explicit `Open()`/`Close()` needed | Do not call `Db.Open()` manually |
| Transactions + async | `BeginTran` / `CommitTran` are synchronous in SqlSugar; wrap async repository calls inside the same `ISqlSugarClient` scope | Use the `UnitOfWork` wrapper shown above; do not span transactions across scopes |
| Soft delete | SqlSugar has no built-in global filter for `deleted_at IS NULL` unlike EF Core's query filters | Add `.Where(x => x.DeletedAt == null)` in every `GetById` / `GetAll` or use a custom Queryable extension |
| `IsJson = true` + nulls | If the DB column is NULL, SqlSugar will deserialize to the C# default (null for reference types, empty list for `List<T>`) — this is safe | Ensure nullable JSONB columns use nullable C# types (`List<T>?` or `T?`) |
| GIN indexes | Not created by SqlSugar's `CodeFirst` migrations for `text[]` or `jsonb` | Write manual migration SQL for GIN indexes |
