---
work_package_id: WP09
title: Search + Rankings API
lane: planned
dependencies:
- WP02
- WP03
subtasks:
- T037
- T038
- T039
phase: Phase 2 - Extended Backend API
assignee: ''
agent: ''
shell_pid: ''
review_status: ''
reviewed_by: ''
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP09 – Search + Rankings API

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP09 --base WP03
```

---

## Objectives & Success Criteria

- Full-text search with zhparser primary / ILIKE fallback; results from all 4 entity types with per-type counts
- Autocomplete returns grouped results (≤3 per type) in <100ms; cached 5 minutes
- Rankings API returns hot榜 (popularity) + 高分榜 (douban_score) for all 3 content types, plus Movie Top100
- All ranking results cached 24h in Redis; refreshed daily by cron job (WP12)

## Context & Constraints

- **Spec**: Scenario 4 (search), FR-18, FR-19, FR-43, FR-44 (search); FR-20, FR-21, FR-45, FR-46 (rankings)
- zhparser may not be available → `_zhparserAvailable` flag checked at startup; ILIKE fallback using `pg_trgm` index
- Top100 gate: `douban_score >= 7.0 AND douban_rating_count >= 1000`
- Rankings 1-3: gold/silver/bronze (frontend renders; API returns `rank` integer)
- Search `type` query param filters to specific content type; absent = all types

## Subtasks & Detailed Guidance

### Subtask T037 – GET /api/v1/search?q= Full-Text Search

**Purpose**: Search across all 4 content types using PostgreSQL FTS with automatic ILIKE fallback.

**Steps**:
1. Create `SearchController.cs` with `[Route("api/v1/search")]`.
2. `SearchApplicationService.SearchAsync(string q, string? type, int page, int pageSize)`:
   a. Validate `q` is non-empty; return empty result if blank.
   b. Check `_zhparserAvailable` flag (set at startup by trying `SELECT 1::tsvector`).
   c. If zhparser available, run FTS query:
      ```sql
      -- For movies table (repeat for tv_series, anime, people):
      SELECT id, 'movie' AS content_type, title_cn, poster_cos_key, douban_score,
             ts_rank(search_vector, query) AS rank_score
      FROM movies,
           plainto_tsquery('chinese_zh', @q) AS query
      WHERE search_vector @@ query
        AND deleted_at IS NULL AND status = 'published'
      ORDER BY rank_score DESC
      ```
   d. If zhparser unavailable, ILIKE fallback:
      ```sql
      SELECT id, 'movie', title_cn, poster_cos_key, douban_score, 0.5 AS rank_score
      FROM movies
      WHERE title_cn ILIKE '%' || @q || '%' OR title_original ILIKE '%' || @q || '%'
        AND deleted_at IS NULL AND status = 'published'
      ```
   e. UNION ALL all 4 queries (movies + tv_series + anime + people); order by rank_score DESC.
   f. Count per type for tab badges.
   g. Apply type filter if provided: only query/return that type's results.
3. `SearchResultDto`:
   ```csharp
   public record SearchResultDto(
       long Id, string ContentType, string TitleCn, int? Year,
       string? PosterCosKey, decimal? DoubanScore,
       string? Synopsis60,  // first 60 chars of synopsis
       double RankScore
   );
   public record SearchResponse(
       List<SearchResultDto> Data,
       Dictionary<string, int> TypeCounts,  // {"movie":18, "tv_series":8, "anime":3, "person":3}
       PaginationDto Pagination
   );
   ```
4. `zhparser` availability check at startup:
   ```csharp
   try {
       await db.Ado.ExecuteCommandAsync("SELECT to_tsvector('chinese_zh', '测试')");
       _zhparserAvailable = true;
   } catch { _zhparserAvailable = false; }
   ```

**Files**:
- `api/src/API/Controllers/SearchController.cs`
- `api/src/Application/Search/SearchApplicationService.cs`
- `api/src/Application/Search/DTOs/SearchResponse.cs`

**Validation**:
- [ ] `GET /api/v1/search?q=星际` returns results from all 4 types
- [ ] `TypeCounts` dictionary has keys for all 4 types (value 0 for empty types)
- [ ] `GET /api/v1/search?q=星际&type=movie` returns only movie results
- [ ] With zhparser disabled, ILIKE fallback still returns results

---

### Subtask T038 – GET /api/v1/search/autocomplete?q= Autocomplete

**Purpose**: Fast autocomplete endpoint returning ≤3 results per content type, grouped, with Redis caching.

**Steps**:
1. `[HttpGet("autocomplete")]` action with `[FromQuery] string q`.
2. `SearchApplicationService.AutocompleteAsync(string q)`:
   a. Check Redis `search:autocomplete:{q}` (5min TTL).
   b. Run 4 parallel queries — TOP 3 per type matching `title_cn ILIKE @q%` (prefix match; faster than FTS for autocomplete):
      ```sql
      SELECT id, title_cn, poster_cos_key FROM movies
      WHERE title_cn LIKE @q || '%' AND deleted_at IS NULL AND status='published'
      ORDER BY popularity DESC LIMIT 3
      ```
   c. For people: `name_cn LIKE @q || '%' OR name_en LIKE @q || '%' LIMIT 3`.
   d. Assemble `AutocompleteResponse`:
      ```csharp
      public record AutocompleteResponse(
          List<AutocompleteItem> Movies,
          List<AutocompleteItem> TvSeries,
          List<AutocompleteItem> Anime,
          List<AutocompleteItem> People,
          string SeeAllUrl
      );
      public record AutocompleteItem(long Id, string ContentType, string TitleCn, string? PosterCosKey, int? Year);
      ```
   e. `SeeAllUrl = $"/search?q={Uri.EscapeDataString(q)}"`.
   f. Cache result; return immediately.
3. Use prefix match (`LIKE @q%`) not full-text search for autocomplete — faster and more intuitive for partial input.

**Files**:
- `api/src/Application/Search/SearchApplicationService.cs` (AutocompleteAsync)
- `api/src/Application/Search/DTOs/AutocompleteResponse.cs`

**Validation**:
- [ ] `GET /api/v1/search/autocomplete?q=星` returns ≤3 items per group
- [ ] Second request for same `q` served from Redis (log confirms cache hit)
- [ ] `see_all_url` is `/search?q=星` (correctly encoded)
- [ ] Empty `q` returns all empty groups (not an error)

---

### Subtask T039 – GET /api/v1/rankings

**Purpose**: Unified rankings endpoint returning hot榜 + high-score榜 for all 3 content types, plus Movie Top100.

**Steps**:
1. `[HttpGet]` action with no params (returns everything; frontend tabs control display).
2. `RankingsApplicationService.GetRankingsAsync()`:
   a. Check Redis for each individual ranking key; build from DB for any that are missing.
   b. Generate all ranking lists:
      - Hot movies (top 50, `popularity DESC`): cache `rankings:movie:hot`
      - Hot TV (top 50): cache `rankings:tv:hot`
      - Hot anime (top 50): cache `rankings:anime:hot`
      - Score movies (top 50, `douban_score DESC NULLS LAST`): cache `rankings:movie:score`
      - Score TV (top 50): cache `rankings:tv:score`
      - Score anime (top 50): cache `rankings:anime:score`
      - Movie Top100 (`douban_score >= 7.0 AND douban_rating_count >= 1000`, top 100, score DESC): cache `rankings:movie:top100`
   c. Add `rank` field (1-based integer) to each result.
3. `RankingsResponse`:
   ```csharp
   public record RankingsResponse(
       List<RankedItemDto> HotMovies,
       List<RankedItemDto> HotTv,
       List<RankedItemDto> HotAnime,
       List<RankedItemDto> ScoreMovies,
       List<RankedItemDto> ScoreTv,
       List<RankedItemDto> ScoreAnime,
       List<RankedItemDto> Top100Movies
   );
   public record RankedItemDto(
       int Rank, long Id, string ContentType, string TitleCn,
       int? Year, string? PosterCosKey, decimal? DoubanScore, int Popularity
   );
   ```
4. All lists cached 24h; refreshed by cron job (WP12). If all Redis keys present, never hits DB.
5. Top100 entry note: gate is `douban_score >= 7.0 AND douban_rating_count >= 1000` (from clarification session 2026-02-19).

**Files**:
- `api/src/API/Controllers/RankingsController.cs`
- `api/src/Application/Rankings/RankingsApplicationService.cs`
- `api/src/Application/Rankings/DTOs/RankingsResponse.cs`

**Validation**:
- [ ] Top100 contains only movies with `douban_score >= 7.0 AND douban_rating_count >= 1000`
- [ ] Each list has `rank` field starting at 1
- [ ] All 7 lists are present in the response (even if some are empty)
- [ ] Hot list respects `popularity` column; score list respects `douban_score`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| UNION ALL search query hits 4 tables — may be slow | Use FTS indexes (GIN on search_vector); ILIKE fallback also has `pg_trgm` GIN index |
| Autocomplete prefix match `LIKE 'q%'` needs B-tree index on title_cn | Add `CREATE INDEX idx_movies_title_cn ON movies(title_cn)` in a future optimization migration |
| Rankings endpoint returns 7 lists × 50–100 items = large payload | Consider splitting into separate per-type endpoints if payload > 100KB; for now unified is simpler |
| `douban_rating_count` may be null for many movies | Add `AND douban_rating_count IS NOT NULL` to Top100 query, or treat NULL as 0 |

## Review Guidance

- Search: `type_counts` shows 0 for types with no results (not missing key)
- Search with zhparser disabled: still returns results via ILIKE (confirm via manual flag toggle)
- Autocomplete: max 3 items per type (no pagination)
- Rankings: `rank` field is 1-based; rank 1 = best; all 7 lists present

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
