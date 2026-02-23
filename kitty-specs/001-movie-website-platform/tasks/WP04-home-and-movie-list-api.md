---
work_package_id: WP04
title: Home + Movie List API
lane: "doing"
dependencies:
- WP02
base_branch: 001-movie-website-platform-WP02
base_commit: 144f7daf3e641e8f67ce316aa5acc55d7d736961
created_at: '2026-02-23T09:17:15.485480+00:00'
subtasks:
- T017
- T018
- T019
- T020
phase: Phase 1 - Core Backend API
assignee: ''
agent: ''
shell_pid: "49352"
review_status: ''
reviewed_by: ''
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP04 – Home + Movie List API

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP04 --base WP03
```

---

## Objectives & Success Criteria

- `GET /api/v1/home` returns active banners + 8 hot items per category (movies, tv, anime-cn, anime-jp)
- `GET /api/v1/movies?genre=sci-fi&region=us&decade=2020s&sort=douban_score` returns filtered, sorted movie list with pagination
- URL query params correctly map to SQL WHERE clauses; TEXT[] filter uses raw SQL with `&&` operator
- All list responses cached in Redis for 10 minutes; cache invalidated on content save
- Response matches `MediaCardDto` schema: `{id, title_cn, year, poster_cos_key, douban_score, genres}`

## Context & Constraints

- **Spec**: Scenarios 1 (home page) and 2 (movie list filtering) define exact expected behavior
- **Contracts**: `kitty-specs/001-movie-website-platform/contracts/movies.yaml` — filter params and response schema
- **FR-5**: Filter state in URL query params; **FR-6**: 24 per page default; **FR-31–FR-35**: filter dimensions
- Rating area must be empty (no text) when `douban_score IS NULL` — API returns null, not 0 or placeholder
- Popularity sort: `popularity DESC`; Score sort: `douban_score DESC NULLS LAST`; Release date sort: earliest CN release from `release_dates` JSONB

## Subtasks & Detailed Guidance

### Subtask T017 – GET /api/v1/home Endpoint

**Purpose**: Aggregate home page data — active banners + hot content sections — into a single API response to minimize frontend round-trips.

**Steps**:
1. Create `HomeController.cs` extending `ControllerBase` (no `[Authorize]`):
   ```csharp
   [ApiController]
   [Route("api/v1/home")]
   public class HomeController(HomeApplicationService svc) : ControllerBase
   {
       [HttpGet]
       public async Task<IActionResult> GetHomeAsync(CancellationToken ct)
           => Ok(await svc.GetHomeDataAsync(ct));
   }
   ```
2. In `HomeApplicationService.GetHomeDataAsync()`:
   a. Check Redis for `CacheKeys.HomeBanners`. If hit, return cached.
   b. Fetch active banners: `WHERE (start_at IS NULL OR start_at <= NOW()) AND (end_at IS NULL OR end_at > NOW())` ordered by `display_order ASC`.
   c. For each banner, JOIN to the relevant content table (based on `content_type`) to get `{title_cn, poster_cos_key, backdrop_cos_key}`.
   d. Fetch hot movies: `SELECT * FROM movies WHERE deleted_at IS NULL AND status='published' ORDER BY popularity DESC LIMIT 8`
   e. Fetch hot TV: same from `tv_series` table.
   f. Fetch hot anime-cn: `WHERE origin='cn'` + popularity sort LIMIT 8.
   g. Fetch hot anime-jp: `WHERE origin='jp'` + popularity sort LIMIT 8.
   h. Assemble `HomeDto` and cache with TTL 10min.
3. `HomeDto` structure:
   ```csharp
   public record HomeDto(
       List<BannerDto> Banners,
       List<MediaCardDto> HotMovies,
       List<MediaCardDto> HotTv,
       List<MediaCardDto> HotAnimeCn,
       List<MediaCardDto> HotAnimeJp
   );
   ```
4. `BannerDto`: `{id, content_type, content_id, title_cn, poster_cos_key, backdrop_cos_key, display_order}`.

**Edge cases**:
- If banners list is empty, return empty array (not null) — frontend uses `.length === 0` check
- If any hot list has < 8 items, return what's available (don't error)

**Files**:
- `api/src/API/Controllers/HomeController.cs`
- `api/src/Application/Home/HomeApplicationService.cs`
- `api/src/Application/Home/HomeDto.cs`

**Validation**:
- [ ] `GET /api/v1/home` returns 200 with `{banners: [], hotMovies: [], hotTv: [], hotAnimeCn: [], hotAnimeJp: []}`
- [ ] Second request within 10min is served from Redis (add log to confirm cache hit)

---

### Subtask T018 – GET /api/v1/movies List Endpoint with All Filters

**Purpose**: Implement the main movie list endpoint supporting all 7 filter dimensions, 3 sort options, and pagination.

**Steps**:
1. Create `MoviesController.cs`:
   ```csharp
   [HttpGet]
   public async Task<IActionResult> GetMoviesAsync([FromQuery] MovieListFilterDto filter, CancellationToken ct)
       => Ok(await _svc.GetMovieListAsync(filter, ct));
   ```
2. `MovieListFilterDto` (maps query params):
   ```csharp
   public record MovieListFilterDto(
       [FromQuery(Name = "genre")] string[]? Genres,
       [FromQuery(Name = "region")] string[]? Regions,
       [FromQuery(Name = "decade")] string? Decade,       // "2020s", "2010s", "2000s", "90s", "earlier"
       [FromQuery(Name = "year")] int? Year,
       [FromQuery(Name = "lang")] string? Language,
       [FromQuery(Name = "score")] decimal? MinScore,     // 7, 8, or 9
       [FromQuery(Name = "sort")] string Sort = "popularity",
       [FromQuery(Name = "page")] int Page = 1,
       [FromQuery(Name = "page_size")] int PageSize = 24
   );
   ```
3. In `MovieApplicationService.GetMovieListAsync()`:
   a. Compute cache key: `CacheKeys.MovieList(MD5Hash(JsonSerializer.Serialize(filter)))`.
   b. Check Redis; if hit return cached `PagedResponse<MediaCardDto>`.
   c. Build SQL query using `ArrayFilterHelper` (T019) for TEXT[] filters.
   d. Apply scalar filters: `douban_score >= @minScore`, `AND deleted_at IS NULL AND status='published'`.
   e. Apply sort: map sort param to ORDER BY clause.
   f. Execute COUNT query for pagination, then SELECT with LIMIT/OFFSET.
   g. Map to `MediaCardDto`; cache result with 10min TTL.
4. `MediaCardDto` mapping: extract year from `release_dates` JSONB first CN date, OR from `first_air_date`, depending on content type. For movies: parse `release_dates` JSON array, find `type='正式公映'` with `region='中国大陆'` date, extract year.

**Response format** (matches contracts/README.md):
```json
{
  "data": [{"id": 1, "title_cn": "...", "year": 2014, ...}],
  "pagination": {"page": 1, "page_size": 24, "total": 120, "total_pages": 5}
}
```

**Files**:
- `api/src/API/Controllers/MoviesController.cs`
- `api/src/Application/Movies/MovieApplicationService.cs` (implement GetMovieListAsync)
- `api/src/Application/Movies/MovieListFilterDto.cs`

**Validation**:
- [ ] `GET /api/v1/movies` returns 200 with paginated array
- [ ] `GET /api/v1/movies?genre=sci-fi` filters to only sci-fi genres
- [ ] `GET /api/v1/movies?decade=2020s` returns movies released 2020–2029
- [ ] `GET /api/v1/movies?score=8` returns movies with `douban_score >= 8.0`
- [ ] `pagination.total_pages = ceil(total / page_size)`

---

### Subtask T019 – Array Filter SQL Helper + Decade Range Conversion

**Purpose**: Centralize the PostgreSQL-specific TEXT[] overlap filter logic, since SqlSugar LINQ cannot translate `&&` array operators.

**Steps**:
1. Create `src/Infrastructure/Persistence/ArrayFilterHelper.cs`:
   ```csharp
   public static class ArrayFilterHelper
   {
       /// <summary>
       /// Build parameterized WHERE clause fragment for TEXT[] array overlap filter.
       /// Returns tuple (sqlFragment, parameters) to append to main query.
       /// </summary>
       public static (string Sql, object Params) BuildArrayOverlap(
           string columnName, string[] values, string paramName)
       {
           return (
               $"{columnName} && @{paramName}::text[]",
               new Dictionary<string, object> { [paramName] = values }
           );
       }

       /// <summary>Convert decade string to (startYear, endYear) range.</summary>
       public static (int Start, int End) DecadeToYearRange(string decade) => decade switch
       {
           "2020s"   => (2020, 2029),
           "2010s"   => (2010, 2019),
           "2000s"   => (2000, 2009),
           "90s"     => (1990, 1999),
           "earlier" => (1888, 1989),
           _         => throw new ArgumentException($"Unknown decade: {decade}")
       };
   }
   ```
2. Usage pattern in `MovieApplicationService`:
   ```csharp
   var whereClauses = new List<string> { "deleted_at IS NULL", "status = 'published'" };
   var parameters = new DynamicParameters();

   if (filter.Genres?.Length > 0)
   {
       whereClauses.Add("genres && @genres::text[]");
       parameters.Add("genres", filter.Genres);
   }
   if (filter.Regions?.Length > 0)
   {
       whereClauses.Add("region && @regions::text[]");
       parameters.Add("regions", filter.Regions);
   }
   if (filter.Decade != null)
   {
       var (start, end) = ArrayFilterHelper.DecadeToYearRange(filter.Decade);
       whereClauses.Add(@"EXISTS (
           SELECT 1 FROM jsonb_array_elements(release_dates) AS rd
           WHERE (rd->>'date')::date BETWEEN @decadeStart AND @decadeEnd
       )");
       parameters.Add("decadeStart", new DateTime(start, 1, 1));
       parameters.Add("decadeEnd", new DateTime(end, 12, 31));
   }
   if (filter.MinScore.HasValue)
   {
       whereClauses.Add("douban_score >= @minScore");
       parameters.Add("minScore", filter.MinScore.Value);
   }
   ```
3. Sort mapping:
   ```csharp
   var orderBy = filter.Sort switch
   {
       "douban_score" => "douban_score DESC NULLS LAST",
       "release_date" => "(SELECT MIN((rd->>'date')::date) FROM jsonb_array_elements(release_dates) rd) DESC NULLS LAST",
       _              => "popularity DESC"  // default: popularity
   };
   ```

**Files**:
- `api/src/Infrastructure/Persistence/ArrayFilterHelper.cs`

**Validation**:
- [ ] `ArrayFilterHelper.DecadeToYearRange("2020s")` returns `(2020, 2029)`
- [ ] `DecadeToYearRange("90s")` returns `(1990, 1999)`
- [ ] Array overlap SQL fragment is parameterized (no string interpolation)

---

### Subtask T020 – Redis Cache Invalidation Strategy

**Purpose**: Define and implement the cache invalidation pattern so stale data never persists beyond edit operations.

**Steps**:
1. After any CREATE/UPDATE of a movie, the Application Layer (NOT the repository) must invalidate cache:
   ```csharp
   // In MovieApplicationService.CreateMovieAsync() and UpdateMovieAsync():
   await _redis.DeleteAsync(CacheKeys.MovieDetail(movie.Id));
   await _redis.DeleteByPatternAsync("movies:list:*");
   await _redis.DeleteAsync(CacheKeys.HomeBanners); // in case movie appears in hot list
   ```
2. Create `CacheInvalidationService` helper to centralize invalidation logic:
   ```csharp
   public class CacheInvalidationService(IRedisCache redis)
   {
       public async Task InvalidateMovieAsync(long movieId)
       {
           await redis.DeleteAsync(CacheKeys.MovieDetail(movieId));
           await redis.DeleteByPatternAsync("movies:list:*");
       }
       public async Task InvalidateTvAsync(long tvId) { ... }
       public async Task InvalidateAnimeAsync(long animeId) { ... }
       public async Task InvalidateHomeAsync() => await redis.DeleteAsync(CacheKeys.HomeBanners);
   }
   ```
3. Document the pattern: **TTL is the last resort**, not the primary mechanism. The goal is zero stale data on admin save.
4. Add a `GET /api/v1/admin/cache/flush` endpoint (admin-only) for emergency cache clear during debugging.

**Files**:
- `api/src/Application/Common/CacheInvalidationService.cs`

**Validation**:
- [ ] After `PUT /api/v1/admin/movies/1`, the next `GET /api/v1/movies/1` returns fresh data (not cached)
- [ ] `movies:list:*` keys are deleted on any movie mutation
- [ ] List cache miss causes fresh DB query + re-cache

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| `jsonb_array_elements` for decade filter is slow without index | Test query plan with EXPLAIN ANALYZE; consider denormalizing release year to a separate integer column if too slow |
| Cache key hash collisions (different filters → same MD5 hash) | MD5 collision probability is negligible for this use case; use full JSON serialization to generate hash |
| `DeleteByPatternAsync` blocks Redis with SCAN on large keyspaces | SCAN is non-blocking in Redis; use `COUNT 100` scan hint to batch; run off main request thread |
| `douban_score IS NULL` shown as 0 in sort | Always use `NULLS LAST` in ORDER BY; API returns null (not 0) in JSON |

## Review Guidance

- `GET /api/v1/home` — verify all 5 sections are present; banners array is empty (not null) when no active banners
- `GET /api/v1/movies?genre=sci-fi` — verify only sci-fi movies returned; check SQL uses `&&` operator
- `GET /api/v1/movies?decade=90s` — verify 1990–1999 range (not decade=1990)
- `GET /api/v1/movies` — verify `pagination.total` is correct COUNT, not just current page count
- Cache hit: second request returns identical response from Redis; confirm via debug log

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
