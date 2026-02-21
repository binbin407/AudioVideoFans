---
work_package_id: WP08
title: People + Awards API
lane: planned
dependencies:
- WP02
- WP03
subtasks:
- T033
- T034
- T035
- T036
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

# Work Package Prompt: WP08 – People + Awards API

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP08 --base WP03
```

---

## Objectives & Success Criteria

- `GET /api/v1/people/:id` returns full person profile with works list (filterable by role) and top-8 collaborators with co-work counts
- `GET /api/v1/people/:id?role=actor` filters works to actor credits only
- `GET /api/v1/awards/:slug` returns award event info with all ceremonies
- `GET /api/v1/awards/:slug/:edition` returns ceremony nominations grouped by category with gold/grey markers
- Person detail cached 1h; awards data is largely static (cached 24h)

## Context & Constraints

- **Spec**: Scenarios 7 (awards), 13 (person detail) — both define exact UI requirements
- **FR-3**: Person page sections: profile, biography (>200 chars fold), works tabs, collaborators, photo wall
- **FR-16**: Top-8 collaborators by co-credit count across all 3 content types
- **FR-17**: Works filtered by role tab (all/director/writer/actor)
- **FR-14**: Awards: at least oscar, golden-globe, cannes, venice, berlin, hkfa, golden-horse — identified by slug
- Award nominations: `is_winner=true` for gold, false for grey; nominations sorted: winners first within category

## Subtasks & Detailed Guidance

### Subtask T033 – GET /api/v1/people/:id Person Detail

**Purpose**: Full person profile including all credits grouped by work.

**Steps**:
1. Create `PeopleController.cs` with `[Route("api/v1/people")]`.
2. Add `[HttpGet("{id:long}")]` action with optional `[FromQuery] string? role` param.
3. `PeopleApplicationService.GetPersonDetailAsync(long id, string? roleFilter)`:
   a. Check Redis `person:detail:{id}:{roleFilter ?? "all"}`.
   b. Fetch `people` by id (soft-delete check).
   c. Fetch all credits for this `person_id` across all content types (JOIN to content tables for title/poster/year):
      ```sql
      SELECT c.role, c.content_type, c.content_id, c.character_name,
             COALESCE(m.title_cn, tv.title_cn, a.title_cn) AS title_cn,
             COALESCE(m.poster_cos_key, tv.poster_cos_key, a.poster_cos_key) AS poster,
             EXTRACT(YEAR FROM COALESCE(m.release_dates[0]->>'date', tv.first_air_date, a.first_air_date)) AS year
      FROM credits c
      LEFT JOIN movies m ON c.content_type='movie' AND c.content_id=m.id
      LEFT JOIN tv_series tv ON c.content_type='tv_series' AND c.content_id=tv.id
      LEFT JOIN anime a ON c.content_type='anime' AND c.content_id=a.id
      WHERE c.person_id = @personId
      ```
   d. Apply role filter if provided: `WHERE c.role = @roleFilter`.
   e. Call `GetTop8CollaboratorsAsync(id)` (T034).
   f. Map to `PersonDetailDto`.
4. `PersonDetailDto`:
   ```csharp
   public record PersonDetailDto(
       long Id, string NameCn, string? NameEn, string[] NameAliases,
       string? Gender, DateOnly? BirthDate, DateOnly? DeathDate,
       string? BirthPlace, string? Nationality, int? HeightCm,
       string[] Professions,
       string? Biography,  // full text; frontend collapses >200 chars
       string? AvatarCosKey,
       string[] PhotosCosKeys,  // empty array when no photos
       List<PersonWorkDto> Works,
       List<CollaboratorDto> TopCollaborators
   );
   public record PersonWorkDto(
       long ContentId, string ContentType, string TitleCn,
       int? Year, string? PosterCosKey, string Role, string? CharacterName
   );
   ```
5. Cache `person:detail:{id}:all` (and per-role variants) 1h.

**Files**:
- `api/src/API/Controllers/PeopleController.cs`
- `api/src/Application/People/PeopleApplicationService.cs`
- `api/src/Application/People/DTOs/PersonDetailDto.cs`

**Validation**:
- [ ] `GET /api/v1/people/888` returns full profile with all sections
- [ ] `photos_cos_keys` is empty array (not null) when person has no photos
- [ ] `GET /api/v1/people/888?role=actor` returns only actor credits

---

### Subtask T034 – Top-8 Collaborators Query

**Purpose**: Find the 8 people who have co-worked most with a given person across all content types.

**Steps**:
1. SQL query strategy:
   ```sql
   SELECT co.person_id, COUNT(*) AS co_count,
          p.name_cn, p.avatar_cos_key
   FROM credits c
   JOIN credits co ON co.content_type = c.content_type
                   AND co.content_id = c.content_id
                   AND co.person_id <> c.person_id
   JOIN people p ON p.id = co.person_id AND p.deleted_at IS NULL
   WHERE c.person_id = @personId
   GROUP BY co.person_id, p.name_cn, p.avatar_cos_key
   ORDER BY co_count DESC
   LIMIT 8
   ```
2. This self-join on `credits` table finds all people who share at least one content with the target person, then aggregates and ranks by count.
3. `CollaboratorDto`:
   ```csharp
   public record CollaboratorDto(
       long PersonId, string NameCn, string? AvatarCosKey, int CoWorkCount
   );
   ```
4. Performance: `credits` has indexes on `(content_type, content_id)` and `(person_id)` — the join is efficient. Add `EXPLAIN ANALYZE` check if credits table exceeds 100K rows.
5. Cache the collaborator result as part of the person detail cache.

**Files**:
- Part of `api/src/Application/People/PeopleApplicationService.cs`

**Validation**:
- [ ] Returns exactly ≤8 collaborators
- [ ] Co-work count is accurate (count of shared content items, not credit rows)
- [ ] Does not include the person themselves in the list

---

### Subtask T035 – GET /api/v1/awards/:slug Awards Main Page

**Purpose**: Award event overview page with all historical ceremony entries.

**Steps**:
1. Add `AwardsController.cs` with `[Route("api/v1/awards")]`.
2. `[HttpGet("{slug}")]` action:
   - Fetch `award_events` by `slug` field; 404 if not found.
   - Fetch all `award_ceremonies` for this event ordered by `edition_number DESC`.
3. `AwardEventDto`:
   ```csharp
   public record AwardEventDto(
       long Id, string NameCn, string? NameEn, string? Description,
       string? OfficialUrl,
       List<CeremonyListItemDto> Ceremonies
   );
   public record CeremonyListItemDto(
       long Id, int EditionNumber, int Year, DateOnly? CeremonyDate
   );
   ```
4. Cache 24h (awards data changes infrequently): `award:event:{slug}`.

**Slug values** (pre-seeded by admin): oscar, golden-globe, cannes, venice, berlin, hkfa (金像奖), golden-horse (金马奖).

**Files**:
- `api/src/API/Controllers/AwardsController.cs`
- `api/src/Application/Awards/AwardsApplicationService.cs`
- `api/src/Application/Awards/DTOs/AwardEventDto.cs`

**Validation**:
- [ ] `GET /api/v1/awards/oscar` returns event info + list of all ceremonies
- [ ] `GET /api/v1/awards/nonexistent` returns 404
- [ ] Ceremonies ordered by `edition_number DESC` (newest first)

---

### Subtask T036 – GET /api/v1/awards/:slug/:edition Ceremony Detail

**Purpose**: Ceremony detail showing all nominations grouped by category with is_winner flag.

**Steps**:
1. `[HttpGet("{slug}/{edition:int}")]` action.
2. `AwardsApplicationService.GetCeremonyDetailAsync(string slug, int edition)`:
   a. Fetch award_event by slug.
   b. Fetch award_ceremony by `event_id + edition_number`; 404 if not found.
   c. Fetch all nominations for this ceremony with JOINs:
      - To movies/tv_series/anime (based on content_type) for poster + title
      - To people for person name + avatar
   d. Group nominations by `category`.
   e. Within each category, sort: winners first (`is_winner DESC`), then alphabetically by content title.
   f. Compute prev/next edition numbers.
3. `CeremonyDetailDto`:
   ```csharp
   public record CeremonyDetailDto(
       long Id, string EventNameCn, int EditionNumber, int Year, DateOnly? Date,
       int? PrevEdition, int? NextEdition,
       List<CategoryNominationsDto> Categories
   );
   public record CategoryNominationsDto(
       string Category,
       List<NominationDto> Nominations
   );
   public record NominationDto(
       long Id, bool IsWinner,
       string? ContentTitleCn, string? PosterCosKey,
       long? PersonId, string? PersonNameCn, string? PersonAvatarCosKey,
       string? Note
   );
   ```
4. Cache `award:ceremony:{slug}:{edition}` 24h.

**Files**:
- `api/src/Application/Awards/AwardsApplicationService.cs` (GetCeremonyDetailAsync)
- `api/src/Application/Awards/DTOs/CeremonyDetailDto.cs`

**Validation**:
- [ ] Nominations grouped by category name
- [ ] Winners (`is_winner=true`) appear before nominations within each category
- [ ] `prev_edition` and `next_edition` correctly determined from `award_ceremonies` table
- [ ] `GET /api/v1/awards/oscar/96` returns edition 96 data

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Collaborator self-join on large credits table | Use existing indexes; add `LIMIT 8` early; test with EXPLAIN |
| Awards multi-table JOIN for content info (movie/tv/anime polymorphic) | Use LEFT JOINs to all 3 tables; use COALESCE for title/poster; accept minor query complexity |
| Person works list can be very large (>500 credits for prolific actors) | Apply `LIMIT 100` to prevent runaway responses; add pagination param in future |

## Review Guidance

- Person detail: `photos_cos_keys` is `[]` (empty array) not null when no photos
- Collaborator count = number of shared content items (each movie/TV/anime they both credited on)
- Awards nomination page: winners appear first in each category
- Prev/next edition uses actual `edition_number` values from DB, not arithmetic

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
