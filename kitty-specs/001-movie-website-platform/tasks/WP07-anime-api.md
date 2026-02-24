---
work_package_id: WP07
title: Anime API
lane: "planned"
dependencies:
- WP02
- WP03
base_branch: 001-movie-website-platform-WP07-merge-base
base_commit: c010a56dcf7f127f65793292406e29ff4290a5f8
created_at: '2026-02-24T01:26:04.658454+00:00'
subtasks:
- T029
- T030
- T031
- T032
phase: Phase 2 - Extended Backend API
assignee: ''
agent: "gpt-5.3-codex"
shell_pid: "28872"
review_status: "has_feedback"
reviewed_by: "binbin407"
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP07 – Anime API

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

**Reviewed by**: binbin407
**Status**: ❌ Changes Requested
**Date**: 2026-02-24

**Issue 1 (blocking): GET /api/v1/anime list does not implement all standard filters declared by ContentListFilter.**

- **Why this blocks approval**: WP07 requires `origin`/`source_material` in addition to all standard filters. Current implementation only forwards `genres`, `year`, `minScore` and omits `regions`, `decade`, `language`.
- **Where**:
  - `api/src/Application/Anime/AnimeApplicationService.cs` in `GetAnimeListAsync` (repository call arguments)
  - `api/src/Infrastructure/Persistence/AnimeRepository.cs` in `GetPagedListAsync` signature and SQL
- **Required fix**:
  1. Extend `IAnimeRepository.GetPagedListAsync` to accept standard filters: `regions`, `decade`, `language` (alongside existing filters).
  2. Pass those filters from `AnimeApplicationService.GetAnimeListAsync`.
  3. Implement SQL predicates in `AnimeRepository` for these filters (consistent with schema and existing filtering conventions).

**Issue 2 (blocking): query parameter name for source material likely mismatches expected API contract.**

- **Why this blocks approval**: WP07 objective and examples use `source_material` in query, but DTO property is `SourceMaterial` without explicit `[FromQuery(Name = "source_material")]`. This can make `?source_material=manga` not bind as expected.
- **Where**:
  - `api/src/Application/Anime/DTOs/AnimeListFilterDto.cs`
- **Required fix**:
  - Ensure request query `source_material` maps correctly (e.g., with `[FromQuery(Name = "source_material")]` on property/parameter model or an equivalent binding-safe approach).

After fixes, re-run:
- `dotnet build api/MovieSite.sln`
- verify list endpoint with `origin` + `source_material` + standard filters combined.


## Implementation Command

```bash
spec-kitty implement WP07 --base WP03
```

---

## Objectives & Success Criteria

- `GET /api/v1/anime` supports `origin` (cn/jp/other) and `source_material` filters in addition to all standard filters
- `GET /api/v1/anime/:id` returns anime-specific fields: `studio`, `source_material`, `origin` with computed `origin_label`; voice actors in credits show `character_name`
- `GET /api/v1/anime/:id/seasons/:n` returns identical structure to TV season detail
- `GET /api/v1/anime/:id/similar` works via SimilarContentService

## Context & Constraints

- **Spec**: Scenarios 10 (anime list) and 11 (anime detail) define exact behavior
- **FR-15**: `origin` field (cn/jp/other); list page has 全部/国漫/日漫 tabs
- **FR-35**: `source_material` filter (original/manga/novel/game) — displayed as separate filter row
- Voice actors (`role = 'voice_actor'`) must be listed in a separate credit section showing `character_name`
- `anime` and `anime_seasons`/`anime_episodes` tables are completely separate from `tv_series` tables

## Subtasks & Detailed Guidance

### Subtask T029 – GET /api/v1/anime List Endpoint

**Purpose**: Anime list with origin and source_material as additional filters.

**Steps**:
1. Create `AnimeController.cs` with `[Route("api/v1/anime")]`.
2. `AnimeListFilterDto`: extends `ContentListFilter` + `string? Origin` (cn/jp/other) + `string? SourceMaterial` (original/manga/novel/game) + `string[]? AirStatus`.
3. In `AnimeApplicationService.GetAnimeListAsync()`:
   a. Build WHERE clauses:
      - `origin = @origin` (single value; list page has tab UI, not multi-select)
      - `source_material = @sourceMaterial`
      - All standard filters via `ArrayFilterHelper`
   b. Sort: douban_score, first_air_date, popularity.
4. `AnimeMediaCardDto` includes: id, title_cn, year, poster_cos_key, douban_score, genres, `origin_label` (国漫/日漫/其他), `source_material_label` (漫画改编/小说改编/游戏改编/原创).
5. Cache `anime:list:{hash}` 10min.

**Label mappings**:
```csharp
private static string OriginLabel(string origin) => origin switch
{
    "cn" => "国漫", "jp" => "日漫", _ => "其他"
};
private static string SourceMaterialLabel(string? sm) => sm switch
{
    "manga"  => "漫画改编", "novel" => "小说改编",
    "game"   => "游戏改编", "original" => "原创", _ => sm ?? ""
};
```

**Files**:
- `api/src/API/Controllers/AnimeController.cs`
- `api/src/Application/Anime/AnimeApplicationService.cs` (GetAnimeListAsync)
- `api/src/Application/Anime/DTOs/AnimeMediaCardDto.cs`

**Validation**:
- [ ] `GET /api/v1/anime?origin=cn` returns only `origin='cn'` anime
- [ ] `GET /api/v1/anime?source=manga` returns only manga-adapted anime
- [ ] Card includes `origin_label` (「国漫」) and `source_material_label` (「漫画改编」)

---

### Subtask T030 – GET /api/v1/anime/:id Detail Endpoint

**Purpose**: Full anime detail with anime-specific production info and voice actor credit section.

**Steps**:
1. `AnimeApplicationService.GetAnimeDetailAsync(long id)`:
   a. Parallel fetches: anime base, seasons summary, credits (separated by role), videos, similar.
   b. Credits grouping — anime-specific: split `voice_actor` role from all others:
      ```csharp
      var voiceActors = credits.Where(c => c.Role == "voice_actor").ToList();
      var directors = credits.Where(c => c.Role == "director").ToList();
      var mainCast = credits.Where(c => c.Role == "actor")
                           .OrderBy(c => c.DisplayOrder).Take(20).ToList();
      ```
   c. `voice_actor` credits MUST include `character_name` prominently.
2. `AnimeDetailDto` extra fields vs TvSeriesDetailDto:
   - `string? Studio` (制作公司)
   - `string? SourceMaterial` (原作类型)
   - `string OriginLabel` (国漫/日漫/其他)
   - `string? SourceMaterialLabel`
   - `List<VoiceActorDto> VoiceActors` — separate from `cast`
3. `VoiceActorDto`:
   ```csharp
   public record VoiceActorDto(
       long PersonId, string NameCn, string? AvatarCosKey,
       string? CharacterName  // 配音角色名
   );
   ```
4. Cache `anime:detail:{id}` 1h.

**Files**:
- `api/src/Application/Anime/AnimeApplicationService.cs` (GetAnimeDetailAsync)
- `api/src/Application/Anime/DTOs/AnimeDetailDto.cs`
- `api/src/Application/Anime/DTOs/VoiceActorDto.cs`

**Validation**:
- [ ] `GET /api/v1/anime/789` includes `studio`, `source_material`, `origin_label` fields
- [ ] `voice_actors` array is separate from `cast` array
- [ ] Each voice actor entry includes `character_name`

---

### Subtask T031 – GET /api/v1/anime/:id/seasons/:n

**Purpose**: Anime season detail — identical structure to TV season detail, but from `anime_seasons` and `anime_episodes` tables.

**Steps**:
1. Add `[HttpGet("{id:long}/seasons/{seasonNumber:int}")]` to `AnimeController`.
2. `AnimeApplicationService.GetSeasonDetailAsync(long animeId, int seasonNumber)`:
   - Fetch from `anime_seasons` WHERE `anime_id = @animeId AND season_number = @seasonNumber`.
   - Fetch all `anime_episodes` for this `season_id` ordered by `episode_number ASC`.
   - Compute prev/next season numbers from `anime_seasons` table.
   - Map to same `SeasonDetailDto` record (can be shared or duplicated).
3. Series title for breadcrumb: fetch `anime.title_cn` for the given `animeId`.
4. `<title>` format for frontend reference: `{动漫名} 第{N}季 - 影视网` (frontend constructs this from `series_title_cn` + `season_number`).

**Files**:
- `api/src/API/Controllers/AnimeController.cs` (add season action)
- `api/src/Application/Anime/AnimeApplicationService.cs` (GetSeasonDetailAsync)

**Validation**:
- [ ] `GET /api/v1/anime/789/seasons/2` returns 12 episodes for season 2
- [ ] Breadcrumb data (`series_title_cn`) included in response
- [ ] `prev_season_number` null for season 1, `next_season_number` null for highest season

---

### Subtask T032 – GET /api/v1/anime/:id/similar

**Purpose**: Similar anime endpoint using SimilarContentService.

**Steps**:
1. Add `[HttpGet("{id:long}/similar")]` to `AnimeController`.
2. Call `_similarContentService.GetSimilarAsync("anime", id, 6)`.
3. Return `{data: AnimeMediaCardDto[]}` including `origin_label` field.

**Files**:
- `api/src/API/Controllers/AnimeController.cs` (add similar action)

**Validation**:
- [ ] Returns ≤ 6 items from `anime` table only (not mixed with movies/TV)
- [ ] Response cards include `origin_label`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Voice actor credits accidentally grouped with regular cast | Explicit `role == 'voice_actor'` check in credit grouping; unit test this |
| `anime_seasons` FK references `anime_id` (not `series_id`) | Use `anime_id` column name in all queries against `anime_seasons` |
| Shared `SeasonDetailDto` between TV and Anime may diverge | Keep shared DTO for now; add discriminator field `content_type` for future flexibility |

## Review Guidance

- `GET /api/v1/anime?origin=cn` — ONLY cn origin items; `?origin=jp` — ONLY jp
- Anime detail: `voice_actors` is a separate top-level array from `cast` and `directors`
- Each voice actor has `character_name` (not null for properly entered data)
- Season detail: uses `anime_seasons`/`anime_episodes` tables, not tv_* tables

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
- 2026-02-24T01:26:08Z – gpt-5.3-codex – shell_pid=13304 – lane=doing – Assigned agent via workflow command
- 2026-02-24T02:57:10Z – gpt-5.3-codex – shell_pid=13304 – lane=for_review – Ready for review: implemented anime list/detail/season/similar API with repository queries, DTOs, caching, and controller endpoints; build passes.
- 2026-02-24T03:00:41Z – gpt-5.3-codex – shell_pid=28872 – lane=doing – Started review via workflow command
- 2026-02-24T03:18:08Z – gpt-5.3-codex – shell_pid=28872 – lane=planned – Moved to planned
