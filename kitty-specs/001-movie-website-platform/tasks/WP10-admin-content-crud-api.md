---
work_package_id: WP10
title: Admin API – Content CRUD
lane: planned
dependencies:
- WP02
- WP03
subtasks:
- T040
- T041
- T042
- T043
- T044
phase: Phase 2 - Admin API
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

# Work Package Prompt: WP10 – Admin API – Content CRUD

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP10 --base WP03
```

---

## Objectives & Success Criteria

- `POST /api/v1/admin/movies` creates movie with `status='published'`, returns new entity with ID
- `PUT /api/v1/admin/movies/:id` updates all fields + credits (delete+re-insert within UoW) + invalidates Redis cache
- `DELETE /api/v1/admin/movies/:id` sets `deleted_at = NOW()` (soft delete); row remains in DB
- Same CRUD for TVSeries, Anime, Person, Franchise
- `GET /api/v1/admin/stats` returns count per content type
- All admin endpoints require `[Authorize]` JWT; return 422 with field errors on validation failure

## Context & Constraints

- **Spec**: FR-24 (admin create→published), FR-25 (edit all fields), FR-26 (soft delete), FR-27 (admin list + keyword search)
- **Constitution**: `[Authorize]` mandatory on all admin controllers; Application Layer owns cache invalidation
- Credits management during update: DELETE existing credits for (content_type, content_id), re-INSERT new credits array within the same UoW transaction
- Seasons/episodes for TV/Anime: managed as sub-resources (`POST /admin/tv/:id/seasons/:n/episodes`)
- FluentValidation for all Create/Update command DTOs; return HTTP 422 on validation failure

## Subtasks & Detailed Guidance

### Subtask T040 – Movie Admin CRUD

**Purpose**: Full CRUD for Movie entities through the admin API.

**Steps**:
1. Create `AdminMoviesController.cs` (extends `AdminControllerBase`):
   - `POST /api/v1/admin/movies` → `CreateMovieCommand`
   - `PUT /api/v1/admin/movies/:id` → `UpdateMovieCommand`
   - `DELETE /api/v1/admin/movies/:id` → `SoftDeleteMovieCommand`
   - `GET /api/v1/admin/movies` → `GetAdminMovieListQuery` (with `?q=keyword` search, `?page=`, `?include_deleted=false`)
   - `GET /api/v1/admin/movies/:id` → `GetAdminMovieDetailQuery` (returns full entity for edit form)
2. `CreateMovieCommand` DTO: all fields from `Movie` entity (title_cn required; all others optional). On create: set `status='published'`, `created_at=NOW()`.
3. `UpdateMovieCommand` DTO: includes `List<CreditInput> Credits` for re-managing credits:
   ```csharp
   public record CreditInput(
       long PersonId, string Role, string? Department,
       string? CharacterName, int DisplayOrder
   );
   ```
   In Application Service:
   ```csharp
   await _uow.BeginAsync(ct);
   await _movieRepo.UpdateAsync(movie, ct);
   await _creditRepo.DeleteByContentAsync("movie", movie.Id, ct);  // delete all existing
   foreach (var c in command.Credits)
       await _creditRepo.AddAsync(MapCredit(c, "movie", movie.Id), ct);
   await _uow.CommitAsync(ct);
   await _cacheInvalidation.InvalidateMovieAsync(movie.Id);
   ```
4. `SoftDeleteMovieCommand`: `UPDATE movies SET deleted_at = NOW() WHERE id = @id`.
5. Admin list with keyword search: `WHERE title_cn ILIKE '%@q%'` (or full-text); support `?include_deleted=true` for showing soft-deleted rows.
6. Validation (FluentValidation):
   ```csharp
   public class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
   {
       public CreateMovieCommandValidator()
       {
           RuleFor(x => x.TitleCn).NotEmpty().MaximumLength(200);
           RuleFor(x => x.DoubanScore).InclusiveBetween(0, 10).When(x => x.DoubanScore.HasValue);
       }
   }
   ```
   Return HTTP 422 with field errors on validation failure.

**Files**:
- `api/src/API/Controllers/Admin/AdminMoviesController.cs`
- `api/src/Application/Admin/Commands/CreateMovieCommand.cs`
- `api/src/Application/Admin/Commands/UpdateMovieCommand.cs`
- `api/src/Application/Admin/Validators/CreateMovieCommandValidator.cs`
- `api/src/Application/Admin/AdminMovieApplicationService.cs`

**Validation**:
- [ ] `POST /admin/movies` with valid body → 201 with new movie id
- [ ] `POST /admin/movies` with empty `title_cn` → 422 with `{errors: {title_cn: ["不能为空"]}}`
- [ ] `DELETE /admin/movies/1` → movie has `deleted_at` set; not returned in public list
- [ ] After `PUT /admin/movies/1`, Redis key `movie:detail:1` is deleted

---

### Subtask T041 – TV Series Admin CRUD + Season/Episode Sub-Resources

**Purpose**: Admin CRUD for TVSeries with nested season/episode management endpoints.

**Steps**:
1. Create `AdminTvController.cs` with same CRUD pattern as movies + additional:
   - `GET/POST /api/v1/admin/tv/:id/seasons` — list/add seasons
   - `PUT/DELETE /api/v1/admin/tv/:id/seasons/:n` — edit/delete specific season
   - `GET/POST /api/v1/admin/tv/:id/seasons/:n/episodes` — list/add episodes
   - `PUT/DELETE /api/v1/admin/tv/:id/seasons/:n/episodes/:e` — edit/delete episode
2. Season create/update: include `poster_cos_key` (COS key from separate upload endpoint), `overview`, `first_air_date`, `vote_average`.
3. Episode create/update: `episode_number`, `name`, `air_date`, `duration_min`, `still_cos_key`, `overview`.
4. On season delete: cascade deletes episodes (handled by DB `ON DELETE CASCADE`).
5. After season/episode add/edit: update parent `tv_series.number_of_seasons` and `number_of_episodes` count fields.
6. Cache invalidation: after any TV season/episode change, invalidate `tv:detail:{id}`.

**Files**:
- `api/src/API/Controllers/Admin/AdminTvController.cs`
- `api/src/Application/Admin/AdminTvApplicationService.cs`

**Validation**:
- [ ] `POST /admin/tv/:id/seasons` creates season; `tv_series.number_of_seasons` increments
- [ ] `DELETE /admin/tv/:id/seasons/:n` cascades to delete all episodes in that season

---

### Subtask T042 – Anime Admin CRUD

**Purpose**: Admin CRUD for Anime with anime-specific fields (origin, studio, source_material) and season/episode management.

**Steps**:
1. `AdminAnimeController.cs`: same structure as AdminTvController with extra anime fields.
2. Create/Update command includes: `string Origin` (required, must be 'cn'/'jp'/'other'), `string? Studio`, `string? SourceMaterial`.
3. Season/episode sub-resources: same endpoints as TV (`/admin/anime/:id/seasons`, etc.) but operating on `anime_seasons`/`anime_episodes` tables.
4. Validation: `origin` must be one of ['cn','jp','other']; `source_material` must be one of ['original','manga','novel','game'] when provided.

**Files**:
- `api/src/API/Controllers/Admin/AdminAnimeController.cs`
- `api/src/Application/Admin/AdminAnimeApplicationService.cs`

**Validation**:
- [ ] `POST /admin/anime` with `origin='invalid'` → 422
- [ ] `POST /admin/anime` with `origin='cn'` → 201

---

### Subtask T043 – Person + Franchise Admin CRUD

**Purpose**: Admin CRUD for Person (with photo management) and Franchise (with movie order management).

**Steps**:
1. `AdminPeopleController.cs`:
   - Standard CRUD + `GET /admin/people?q=keyword`
   - Person Update includes `photos_cos_keys: string[]` (replace full array on update)
   - Soft delete: set `deleted_at` on person; existing credits remain (don't cascade delete credits)
2. `AdminFranchisesController.cs`:
   - Standard CRUD for `franchises` table
   - Extra endpoint: `PUT /admin/franchises/:id/movie-order` — accepts `List<{movie_id, franchise_order}>` and bulk-updates `movies.franchise_order` field; include `franchise_id` update if needed
3. `POST /admin/upload`: COS file upload endpoint (returns `cos_key`):
   - Accepts multipart form data
   - Validates file type (jpg/png/webp only)
   - Uploads to COS via `ITencentCosClient.UploadAsync()`
   - Returns `{cos_key: "posters/xxx.jpg"}`
   - Protected with `[Authorize]`

**Files**:
- `api/src/API/Controllers/Admin/AdminPeopleController.cs`
- `api/src/API/Controllers/Admin/AdminFranchisesController.cs`
- `api/src/API/Controllers/Admin/AdminUploadController.cs`
- `api/src/Application/Admin/AdminPeopleApplicationService.cs`

**Validation**:
- [ ] `POST /admin/people` creates person; `photos_cos_keys` stored as TEXT[]
- [ ] `PUT /admin/franchises/:id/movie-order` updates franchise_order for each movie
- [ ] `POST /admin/upload` with non-image file → 400

---

### Subtask T044 – GET /api/v1/admin/stats + Content Search

**Purpose**: Dashboard statistics and admin-side keyword search for content list pages.

**Steps**:
1. `AdminController.GetStatsAsync()`:
   ```csharp
   var stats = new {
       movies = await _db.Queryable<Movie>().Where(m => m.DeletedAt == null).CountAsync(),
       tv_series = await _db.Queryable<TvSeries>().Where(t => t.DeletedAt == null).CountAsync(),
       anime = await _db.Queryable<Anime>().Where(a => a.DeletedAt == null).CountAsync(),
       people = await _db.Queryable<Person>().Where(p => p.DeletedAt == null).CountAsync(),
       pending = await _db.Queryable<PendingContent>().Where(p => p.ReviewStatus == "pending").CountAsync()
   };
   ```
2. Admin list search (already stubbed in T040–T043): `WHERE title_cn ILIKE '%@q%'` with `?q=` param.
3. Add `?include_deleted=true` support to all admin list endpoints (shows soft-deleted rows with `deleted_at` displayed).

**Files**:
- `api/src/API/Controllers/Admin/AdminController.cs` (stats endpoint)

**Validation**:
- [ ] `GET /admin/stats` returns counts for all 5 categories
- [ ] `GET /admin/movies?q=星际` returns only movies with 星际 in title
- [ ] `GET /admin/movies?include_deleted=true` includes soft-deleted movies

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Credits delete+re-insert not atomic | Always use UoW transaction; rollback on any failure |
| Photo upload to COS from server adds latency | Upload asynchronously; COS SDK supports async; consider client-side direct COS upload as optimization |
| `number_of_seasons`/`number_of_episodes` denormalized counts drift | Recalculate from season/episode count on every season/episode add/delete |

## Review Guidance

- `POST /admin/movies` always creates with `status='published'`; no approval needed for admin-created content (FR-24)
- Credits are fully replaced on update (not merged) — delete all + re-insert
- Soft delete: `deleted_at` is set; row remains queryable with `include_deleted=true`
- Stats counts exclude soft-deleted rows

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
