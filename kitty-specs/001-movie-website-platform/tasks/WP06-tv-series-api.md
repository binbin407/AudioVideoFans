---
work_package_id: WP06
title: TV Series API
lane: "doing"
dependencies:
- WP02
- WP03
base_branch: 001-movie-website-platform-WP06-merge-base
base_commit: c010a56dcf7f127f65793292406e29ff4290a5f8
created_at: '2026-02-23T13:56:22.064553+00:00'
subtasks:
- T025
- T026
- T027
- T028
phase: Phase 2 - Extended Backend API
assignee: ''
agent: "gpt-5.3-codex"
shell_pid: "62760"
review_status: ''
reviewed_by: ''
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP06 – TV Series API

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP06 --base WP03
```

---

## Objectives & Success Criteria

- `GET /api/v1/tv` returns filtered TV series list with `air_status` multi-value filter support
- `GET /api/v1/tv/:id` returns full detail including seasons summary (for accordion), `next_episode_info` only when airing
- `GET /api/v1/tv/:id/seasons/:n` returns complete season with all episodes and prev/next season numbers
- `GET /api/v1/tv/:id/similar` returns up to 6 similar TV series
- All responses follow the same caching + invalidation patterns established in WP04

## Context & Constraints

- **Spec**: Scenarios 8 (TV list) and 9 (TV detail) define exact behavior
- **FR-10**: Season/episode two-level hierarchy; season detail page includes prev/next navigation
- **FR-42**: `still_cos_key` on episodes; `vote_average` on seasons
- Season accordion data: the list endpoint returns all seasons with summary only (not full episode list); episode list comes only from season detail endpoint
- `next_episode_info` JSONB: `{air_date, title, season_number, episode_number}` — only include in response when `air_status = 'airing'` AND JSON is not null

## Subtasks & Detailed Guidance

### Subtask T025 – GET /api/v1/tv List Endpoint

**Purpose**: TV series list with all standard filters plus TV-specific `air_status` multi-value filter.

**Steps**:
1. Create `TvController.cs` with `[Route("api/v1/tv")]`.
2. `TvListFilterDto`: extends `ContentListFilter` + `string[]? AirStatus` (multi-value: `?status=airing&status=ended`).
3. In `TvSeriesApplicationService.GetTvListAsync()`:
   a. Build WHERE clauses using `ArrayFilterHelper` for genres/region/language.
   b. Add `air_status = ANY(@statuses)` when `AirStatus` is provided (use PostgreSQL `ANY` with array param).
   c. Sort options: `douban_score DESC NULLS LAST`, `first_air_date DESC NULLS LAST`, `popularity DESC`.
   d. `MediaCardDto` for TV includes additional `air_status` field (「连载中」/「已完结」etc.) for card badge.
4. Cache key: `CacheKeys.TvList(MD5Hash(filter))` with 10min TTL.
5. Return `PagedResponse<TvMediaCardDto>` where `TvMediaCardDto` extends `MediaCardDto` with `string? AirStatus`.

**SQL fragment for air_status multi-filter**:
```sql
-- When multiple status values provided:
air_status = ANY(@statuses::varchar[])
```

**Files**:
- `api/src/API/Controllers/TvController.cs`
- `api/src/Application/TvSeries/TvSeriesApplicationService.cs` (GetTvListAsync)
- `api/src/Application/TvSeries/DTOs/TvMediaCardDto.cs`

**Validation**:
- [ ] `GET /api/v1/tv?status=airing&status=ended` returns only airing OR ended series
- [ ] `GET /api/v1/tv?sort=first_air_date` sorts by first air date descending
- [ ] Card includes `air_status` field for frontend badge display

---

### Subtask T026 – GET /api/v1/tv/:id Detail Endpoint

**Purpose**: Full TV series detail including season summary list (for accordion) and next episode info.

**Steps**:
1. `TvSeriesApplicationService.GetTvDetailAsync(long id)`:
   a. Check Redis `tv:detail:{id}`; return cached if present.
   b. Fetch `tv_series` by id.
   c. Parallel fetch:
      - Credits (directors + main cast top 20)
      - All seasons for this series (ordered by `season_number ASC`) — season summary only (no episodes list here)
      - Videos
      - Similar (call SimilarContentService)
   d. For each season in the accordion summary:
      ```csharp
      new SeasonSummaryDto(
          Id: season.Id,
          SeasonNumber: season.SeasonNumber,
          Name: season.Name,
          EpisodeCount: season.EpisodeCount,
          FirstAirDate: season.FirstAirDate,
          PosterCosKey: season.PosterCosKey,   // may be null → frontend shows grey placeholder
          Overview: season.Overview,           // truncated server-side to 200 chars? No — frontend truncates with CSS
          VoteAverage: season.VoteAverage      // null if no rating
      )
      ```
   e. `next_episode_info`: only include in response if `tv_series.AirStatus == 'airing' && tv_series.NextEpisodeInfo != null`.
2. `TvSeriesDetailDto` fields:
   - All base fields: title_cn/en, aliases, synopsis, genres, region, language, first_air_date, last_air_date, air_status (label), number_of_seasons, number_of_episodes, douban_score, imdb_score, poster_cos_key, backdrop_cos_key, extra_posters, extra_backdrops
   - `SeasonsSummary: List<SeasonSummaryDto>`
   - `NextEpisodeInfo: NextEpisodeInfoDto?` (null when not airing or no info)
   - Cast, directors, videos, similar
3. Cache with 1h TTL.

**Files**:
- `api/src/Application/TvSeries/TvSeriesApplicationService.cs` (GetTvDetailAsync)
- `api/src/Application/TvSeries/DTOs/TvSeriesDetailDto.cs`
- `api/src/Application/TvSeries/DTOs/SeasonSummaryDto.cs`

**Validation**:
- [ ] `GET /api/v1/tv/456` when `air_status = 'airing'` includes `next_episode_info`
- [ ] `GET /api/v1/tv/456` when `air_status = 'ended'` has `next_episode_info: null`
- [ ] `seasons_summary` array sorted by `season_number ASC`
- [ ] Season without `poster_cos_key` returns `null` (not empty string) for frontend placeholder logic

---

### Subtask T027 – GET /api/v1/tv/:id/seasons/:n Season Detail

**Purpose**: Full season detail with all episodes and prev/next season navigation.

**Steps**:
1. Add `[HttpGet("{id:long}/seasons/{seasonNumber:int}")]` to `TvController`.
2. `TvSeriesApplicationService.GetSeasonDetailAsync(long seriesId, int seasonNumber)`:
   a. Fetch `tv_seasons` WHERE `series_id = @seriesId AND season_number = @seasonNumber`; 404 if not found.
   b. Fetch all `tv_episodes` for this `season_id` ordered by `episode_number ASC`.
   c. Fetch series title (for breadcrumb).
   d. Compute prev/next season: `SELECT MIN(season_number), MAX(season_number) FROM tv_seasons WHERE series_id = @seriesId`; then check adjacent season numbers.
3. `SeasonDetailDto`:
   ```csharp
   public record SeasonDetailDto(
       long Id, string SeriesId, string SeriesTitleCn,  // for breadcrumb
       int SeasonNumber, string? Name, int EpisodeCount,
       DateOnly? FirstAirDate, string? PosterCosKey,
       string? Overview,  // full text, not truncated
       decimal? VoteAverage,
       List<EpisodeDto> Episodes,
       int? PrevSeasonNumber,  // null if this is season 1
       int? NextSeasonNumber   // null if this is latest season
   );

   public record EpisodeDto(
       long Id, int EpisodeNumber, string? Name,
       DateOnly? AirDate, int? DurationMin,
       string? StillCosKey, string? Overview, decimal? VoteAverage
   );
   ```
4. Episode code format for frontend: `S{seasonNumber:D2}E{episodeNumber:D2}` (e.g., `S03E06`) — return both `season_number` and `episode_number` integers; frontend formats the code.
5. `PrevSeasonNumber`: if `seasonNumber > minSeasonNumber` then `seasonNumber - 1` else null. Note: use actual existing season numbers (not just arithmetic minus 1, in case seasons are non-contiguous).

**Files**:
- `api/src/API/Controllers/TvController.cs` (add season action)
- `api/src/Application/TvSeries/TvSeriesApplicationService.cs` (GetSeasonDetailAsync)
- `api/src/Application/TvSeries/DTOs/SeasonDetailDto.cs`

**Validation**:
- [ ] `GET /api/v1/tv/456/seasons/3` returns 10 episodes for season 3
- [ ] `prev_season_number` is null when requesting season 1
- [ ] `next_season_number` is null when requesting the highest-numbered season
- [ ] `overview` is the complete non-truncated text

---

### Subtask T028 – GET /api/v1/tv/:id/similar

**Purpose**: Similar TV series endpoint (reuses SimilarContentService from WP05).

**Steps**:
1. Add `[HttpGet("{id:long}/similar")]` action to `TvController`.
2. Call `_similarContentService.GetSimilarAsync("tv_series", id, limit: 6)`.
3. Return `{data: MediaCardDto[]}` — same schema as movie similar.
4. Include `air_status` in the TV media cards returned by similar service (extend MediaCardDto mapping).

**Files**:
- `api/src/API/Controllers/TvController.cs` (add similar action)

**Validation**:
- [ ] Returns ≤ 6 items; never returns the requested series itself
- [ ] Similar series are `status='published'` and `deleted_at IS NULL`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Season numbers may not be sequential (e.g., 1, 2, 5) | Use actual MIN/MAX + next/prev by querying adjacent existing season numbers, not arithmetic |
| `next_episode_info` JSONB might be invalid JSON | Use try/catch around JSON deserialization; return null on parse error |
| Large season with 50+ episodes | No pagination needed for episodes within a season (spec shows full list); paginate if > 100 episodes in future |

## Review Guidance

- TV list response includes `air_status` field on each card for badge rendering
- TV detail `next_episode_info` is null when not airing — not an empty object
- Season detail `overview` is full text (not CSS-truncated — that's the frontend's job)
- Prev/next season uses real existing season numbers (query DB for adjacent seasons)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
- 2026-02-23T13:56:24Z – gpt-5.3-codex – shell_pid=33576 – lane=doing – Assigned agent via workflow command
- 2026-02-23T14:33:02Z – gpt-5.3-codex – shell_pid=33576 – lane=for_review – Ready for review: completed T025/T026/T027/T028/T131, API builds successfully.
- 2026-02-23T14:34:49Z – gpt-5.3-codex – shell_pid=62760 – lane=doing – Started review via workflow command
