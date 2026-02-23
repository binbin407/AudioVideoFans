---
work_package_id: WP05
title: Movie Detail API
lane: "doing"
dependencies: [WP04]
base_branch: 001-movie-website-platform-WP04
base_commit: 12a010785208453e66f26f3d6c45e736eceb7169
created_at: '2026-02-23T12:20:54.910939+00:00'
subtasks:
- T021
- T022
- T023
- T024
phase: Phase 1 - Core Backend API
assignee: ''
agent: "gpt-5.3-codex"
shell_pid: "67572"
review_status: ''
reviewed_by: ''
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP05 – Movie Detail API

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP05 --base WP04
```

---

## Objectives & Success Criteria

- `GET /api/v1/movies/:id` returns full MovieDetailDto including franchise block (null when no franchise), awards, videos, cast (top 20), directors, extra images, similar content (6 items)
- `GET /api/v1/movies/:id/credits` returns grouped credits (directors / writers / cast / others) with all person info
- `SimilarContentService` returns up to 6 similar movies ordered by keyword overlap DESC, then `douban_score DESC`
- `GET /api/v1/franchises/:id` returns franchise info + movies list ordered by `franchise_order ASC`
- Detail endpoint cached in Redis for 1 hour; cache invalidated on content edit

## Context & Constraints

- **Spec**: Scenario 3 (movie detail page) — all sections must be represented in the API response
- **Contracts**: `kitty-specs/001-movie-website-platform/contracts/movies.yaml` — `MovieDetail` schema
- **FR-8**: Franchise block only present when `franchise_id IS NOT NULL`; `franchise.total` = COUNT of all movies in franchise
- **FR-39**: extra_posters + extra_backdrops arrays returned for image tab block
- **FR-41**: Similar content: keyword overlap first, genre fallback; never include self; only published, non-deleted

## Subtasks & Detailed Guidance

### Subtask T021 – GET /api/v1/movies/:id Full Detail

**Purpose**: Assemble the complete movie detail response from multiple tables (movie, credits, awards, videos, keywords, similar).

**Steps**:
1. `MovieApplicationService.GetMovieDetailAsync(long id)`:
   a. Check Redis `movie:detail:{id}`; return if cached.
   b. Fetch movie by id (include soft-delete check: `deleted_at IS NULL AND status='published'`); return 404 if not found.
   c. Parallel fetches (use `Task.WhenAll` for speed):
      - Credits: top 20 cast + all directors from `credits` table, ordered by `display_order`
      - Awards: JOIN nominations → ceremonies → events for this movie_id
      - Videos: all `media_videos` for content_type='movie', content_id=id
      - Franchise: if `movie.FranchiseId != null`, fetch `franchises` + count of movies in franchise
      - Similar: call `SimilarContentService` (T023)
   d. Map to `MovieDetailDto`; set in Redis with 1h TTL.
2. Franchise block mapping:
   ```csharp
   FranchiseRef? franchise = null;
   if (movie.FranchiseId.HasValue)
   {
       var f = await _franchiseRepo.GetByIdAsync(movie.FranchiseId.Value, ct);
       var total = await _movieRepo.CountByFranchiseIdAsync(movie.FranchiseId.Value, ct);
       franchise = new FranchiseRef(f.Id, f.NameCn, movie.FranchiseOrder ?? 0, total);
   }
   ```
3. Awards mapping: group by `ceremony.edition_number` + `event.name_cn`, include `is_winner` flag:
   ```csharp
   awards = nominations
       .Select(n => new AwardItemDto(n.AwardEvent.NameCn, n.Ceremony.EditionNumber, n.Category, n.IsWinner))
       .ToList();
   ```
4. `douban_rating_dist` returned as-is from JSONB (5-star percentages object).

**MovieDetailDto fields** (see contracts/movies.yaml for full schema):
- All base fields from `movies` table
- `franchise: FranchiseRef?` (null when no franchise)
- `cast: CreditPersonDto[]` (top 20, with `character_name`)
- `directors: CreditPersonDto[]`
- `awards: AwardItemDto[]`
- `videos: VideoDto[]` (grouped by type)
- `extra_posters: string[]`, `extra_backdrops: string[]`
- `similar: MediaCardDto[]` (6 items)

**Files**:
- `api/src/Application/Movies/MovieApplicationService.cs` (implement GetMovieDetailAsync)
- `api/src/Application/Movies/DTOs/MovieDetailDto.cs`
- `api/src/Application/Movies/DTOs/FranchiseRef.cs`
- `api/src/Application/Movies/DTOs/AwardItemDto.cs`

**Validation**:
- [ ] `GET /api/v1/movies/1` returns 200 with `franchise: null` when movie has no franchise
- [ ] `franchise.total` equals actual count of movies with `franchise_id = movie.franchise_id`
- [ ] Awards array empty (not null) when no nominations found
- [ ] Second call within 1h served from Redis

---

### Subtask T022 – GET /api/v1/movies/:id/credits

**Purpose**: Return full credits list grouped by department for the "全部演职员" page.

**Steps**:
1. Add `[HttpGet("{id}/credits")]` action to `MoviesController`.
2. Fetch all credits for `content_type='movie', content_id=id` with JOIN to `people` table.
3. Group by `department` or `role`:
   ```csharp
   var grouped = credits.GroupBy(c => c.Role switch
   {
       "director" => "directors",
       "writer"   => "writers",
       "actor"    => "cast",
       "producer" => "producers",
       _          => "others"
   });
   ```
4. Return `CreditsResponseDto`:
   ```csharp
   public record CreditsResponseDto(
       List<CreditPersonDto> Directors,
       List<CreditPersonDto> Writers,
       List<CreditPersonDto> Cast,
       List<CreditPersonDto> Producers,
       List<CreditPersonDto> Others
   );
   ```
5. Each `CreditPersonDto`: `{person_id, name_cn, name_en, avatar_cos_key, character_name, display_order}`.
6. Sort each group by `display_order ASC`.
7. No Redis cache for this endpoint (accessed infrequently; direct DB query is fine).

**Files**:
- `api/src/API/Controllers/MoviesController.cs` (add credits action)
- `api/src/Application/Movies/DTOs/CreditsResponseDto.cs`

**Validation**:
- [ ] Returns 200 with `{directors: [...], writers: [...], cast: [...], ...}`
- [ ] Voice actors in anime use `role = 'voice_actor'` → appear in `others` group for movies (voice actors not expected in movie credits)
- [ ] All groups present even when empty (empty arrays, not null)

---

### Subtask T023 – SimilarContentService (Keyword + Genre Overlap)

**Purpose**: Implement the shared similar content service used by all 3 content types (movie, tv_series, anime) to find up to 6 related items.

**Steps**:
1. Create `src/Application/Common/SimilarContentService.cs`:
   ```csharp
   public class SimilarContentService(ISqlSugarClient db)
   {
       public async Task<List<MediaCardDto>> GetSimilarAsync(
           string contentType, long contentId, int limit = 6)
       {
           // 1. Get target content keywords
           // 2. Find similar by keyword overlap (JOIN content_keywords)
           // 3. If < limit results, fill with genre overlap
           // 4. Return up to limit items as MediaCardDto
       }
   }
   ```
2. Primary query (keyword overlap):
   ```sql
   WITH target_kw AS (
     SELECT keyword_id FROM content_keywords
     WHERE content_type = @contentType AND content_id = @contentId
   )
   SELECT m.id, m.title_cn, m.poster_cos_key, m.douban_score,
          COUNT(ck.keyword_id) AS kw_overlap
   FROM movies m  -- swap table based on contentType
   LEFT JOIN content_keywords ck ON ck.content_type = @contentType
       AND ck.content_id = m.id
       AND ck.keyword_id IN (SELECT keyword_id FROM target_kw)
   WHERE m.id <> @contentId AND m.deleted_at IS NULL AND m.status = 'published'
   GROUP BY m.id
   ORDER BY kw_overlap DESC, m.douban_score DESC NULLS LAST
   LIMIT @limit
   ```
3. If keyword overlap returns fewer than `limit` results, run genre overlap fallback:
   ```sql
   -- Get target genres first, then find movies sharing at least 1 genre
   SELECT * FROM movies WHERE genres && @targetGenres::text[]
     AND id <> @contentId AND deleted_at IS NULL AND status = 'published'
     AND id NOT IN (@alreadyFound)
   ORDER BY douban_score DESC NULLS LAST
   LIMIT @remaining
   ```
4. Register as a scoped service in DI.
5. Handle case where content has no keywords: skip to genre fallback immediately.

**Files**:
- `api/src/Application/Common/SimilarContentService.cs`

**Validation**:
- [ ] Returns exactly 6 items (or fewer if not enough similar content exists)
- [ ] Does not include the source content itself
- [ ] Only returns `status='published'` + `deleted_at IS NULL` items
- [ ] Works for all 3 content types (movie/tv_series/anime)

---

### Subtask T024 – GET /api/v1/franchises/:id

**Purpose**: Implement the franchise detail endpoint showing series metadata and all movies in the series ordered by their sequence number.

**Steps**:
1. Create `FranchisesController.cs`:
   ```csharp
   [ApiController]
   [Route("api/v1/franchises")]
   public class FranchisesController(FranchiseApplicationService svc) : ControllerBase
   {
       [HttpGet("{id:long}")]
       public async Task<IActionResult> GetFranchiseAsync(long id, CancellationToken ct)
       {
           var result = await svc.GetFranchiseDetailAsync(id, ct);
           return result != null ? Ok(result) : NotFound(new { error = new { code = "NOT_FOUND", message = "系列不存在" } });
       }
   }
   ```
2. `FranchiseApplicationService.GetFranchiseDetailAsync(long id)`:
   a. Fetch `franchises` by id (soft-delete check).
   b. Fetch all movies with `franchise_id = id` and `deleted_at IS NULL` and `status = 'published'`, ordered by `franchise_order ASC NULLS LAST`.
   c. Map to `FranchiseDetailDto`:
      ```csharp
      public record FranchiseDetailDto(
          long Id, string NameCn, string? NameEn,
          string? Overview, string? PosterCosKey,
          int TotalMovies,
          List<FranchiseMovieDto> Movies
      );
      public record FranchiseMovieDto(
          long Id, string TitleCn, int? Year,
          string? PosterCosKey, decimal? DoubanScore, int? SequenceNumber
      );
      ```
3. `SequenceNumber` = `movie.FranchiseOrder` (the `franchise_order` column).
4. `Year`: extract from `release_dates` JSONB first CN date OR the earliest date in the array.
5. For `overview` exceeding 200 chars: return full text from API (frontend handles truncation per FR-8 spec).

**Files**:
- `api/src/API/Controllers/FranchisesController.cs`
- `api/src/Application/Franchises/FranchiseApplicationService.cs`
- `api/src/Application/Franchises/DTOs/FranchiseDetailDto.cs`

**Validation**:
- [ ] `GET /api/v1/franchises/1` returns movies ordered by `franchise_order ASC`
- [ ] `total_movies` matches actual count of non-deleted published movies in franchise
- [ ] Movie with `franchise_order = null` appears after all numbered movies
- [ ] `GET /api/v1/franchises/99999` returns 404

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| N+1 queries when building MovieDetailDto | Use `Task.WhenAll` for parallel fetches; never query credits inside a movie loop |
| Similar content query slow without indexes | `idx_content_keywords_content` on `(content_type, content_id)` handles this; verify EXPLAIN |
| `franchise.total` counting draft/deleted movies | Use `WHERE deleted_at IS NULL AND status='published'` in count query |
| Awards JOIN may return many rows for popular movies | Use `.Take(50)` limit in awards fetch; frontend collapses > 5 items |

## Review Guidance

- Movie with `franchise_id = null`: `franchise` field is `null` (not `{}`)
- Movie with `franchise_id` set: `franchise.order` shows sequence; `franchise.total` shows total franchise movie count
- `extra_posters` and `extra_backdrops` are arrays (not null, but can be empty `[]`)
- Credits endpoint: all 5 groups present even if empty
- Similar: never returns the target movie itself

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
- 2026-02-23T12:20:57Z – gpt-5.3-codex – shell_pid=60880 – lane=doing – Assigned agent via workflow command
- 2026-02-23T13:12:37Z – gpt-5.3-codex – shell_pid=60880 – lane=for_review – Ready for review: implemented movie detail, credits grouping, similar content service, and franchise detail API with build passing
- 2026-02-23T13:20:47Z – gpt-5.3-codex – shell_pid=67572 – lane=doing – Started review via workflow command
