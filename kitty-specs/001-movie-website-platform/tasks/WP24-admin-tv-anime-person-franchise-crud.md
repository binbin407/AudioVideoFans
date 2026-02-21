---
work_package_id: "WP24"
subtasks:
  - "T103"
  - "T104"
  - "T105"
  - "T106"
title: "Admin TV, Anime, Person + Franchise CRUD"
phase: "Phase 5 - Admin Frontend"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP23"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Prompt generated via /spec-kitty.tasks"
---

# Work Package Prompt: WP24 – Admin TV, Anime, Person + Franchise CRUD

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP24 --base WP23
```

---

## Objectives & Success Criteria

- TV series admin: list + create/edit form + season/episode sub-resource management UI
- Anime admin: same as TV with extra origin/studio/source_material fields
- Person admin: list + create/edit form with photo management (multi-image upload)
- Franchise admin: list + create/edit + movie order drag-and-drop reordering

## Context & Constraints

- **Spec**: FR-25 (edit all fields), FR-26 (soft delete), FR-27 (admin list)
- TV/Anime season management: nested table within series edit page (not separate route)
- Person photos: stored as `photos_cos_keys: string[]`; upload multiple via `POST /admin/upload`
- Franchise movie order: drag-and-drop list using TDesign `t-table` drag feature or `vuedraggable`
- Reuse `MovieFormPage` patterns (same TDesign form structure)

## Subtasks & Detailed Guidance

### Subtask T103 – TV Series Admin CRUD

**Purpose**: Admin list and form for TV series with season/episode management.

**Steps**:
1. `src/pages/tv/TvListPage.vue`: same pattern as `MovieListPage` with `air_status` column.
2. `src/pages/tv/TvFormPage.vue`:
   - All TV fields: `title_cn`, `synopsis`, `genres`, `region`, `air_status` (select), `first_air_date`, `last_air_date`, `number_of_seasons`, `number_of_episodes`
   - Credits section: same as movie form
   - Seasons sub-section (edit mode only): TDesign `t-table` listing seasons
     - Each row: season number + name + episode count + actions (编辑/删除)
     - "添加季" button → inline form or dialog: season number, name, overview, first_air_date, poster upload
     - Episode management: expand season row → episode list table with "添加集" button
3. Season add/edit: `POST /admin/tv/:id/seasons` / `PUT /admin/tv/:id/seasons/:n`.
4. Episode add/edit: `POST /admin/tv/:id/seasons/:n/episodes` / `PUT /admin/tv/:id/seasons/:n/episodes/:e`.

**Files**:
- `admin/src/pages/tv/TvListPage.vue`
- `admin/src/pages/tv/TvFormPage.vue`

**Validation**:
- [ ] Adding a season increments `number_of_seasons` (visible after refresh)
- [ ] Episode table shows within expanded season row
- [ ] TV form submits with credits

---

### Subtask T104 – Anime Admin CRUD

**Purpose**: Admin list and form for anime with anime-specific fields.

**Steps**:
1. `src/pages/anime/AnimeListPage.vue`: same pattern as TV list.
2. `src/pages/anime/AnimeFormPage.vue`: extends TV form with:
   - `origin` (select: 国漫/日漫/其他 → cn/jp/other) — required
   - `studio` (text input)
   - `source_material` (select: 原创/漫画改编/小说改编/游戏改编 → original/manga/novel/game)
   - Season/episode management: same as TV but calls `/admin/anime/:id/seasons` endpoints
3. Validation: `origin` required; show 422 error if missing.

**Files**:
- `admin/src/pages/anime/AnimeListPage.vue`
- `admin/src/pages/anime/AnimeFormPage.vue`

**Validation**:
- [ ] Origin field required; form won't submit without it
- [ ] Season management uses `/admin/anime/` API paths (not `/admin/tv/`)
- [ ] Source material select shows correct options

---

### Subtask T105 – Person Admin CRUD

**Purpose**: Admin list and form for people with multi-photo management.

**Steps**:
1. `src/pages/people/PeopleListPage.vue`: columns: ID, 姓名(CN), 姓名(EN), 职业, 操作.
2. `src/pages/people/PersonFormPage.vue`:
   - Fields: `name_cn` (required), `name_en`, `name_aliases` (tag input), `gender` (select), `birth_date`, `death_date`, `birth_place`, `nationality`, `height_cm`, `professions` (multi-select: 导演/演员/编剧/制片人/配音)
   - `biography` (textarea)
   - Avatar upload: single image → `POST /admin/upload` → `avatar_cos_key`
   - Photos upload: multiple images → multiple `POST /admin/upload` calls → `photos_cos_keys[]`
     - Show thumbnail grid of uploaded photos; click × to remove
3. Soft delete: same confirm dialog pattern as movies.

**Files**:
- `admin/src/pages/people/PeopleListPage.vue`
- `admin/src/pages/people/PersonFormPage.vue`

**Validation**:
- [ ] Multiple photos can be uploaded; each shows thumbnail with remove button
- [ ] Removing a photo updates `photos_cos_keys` array
- [ ] Person form submits with all fields

---

### Subtask T106 – Franchise Admin CRUD + Movie Order

**Purpose**: Franchise management with drag-and-drop movie ordering.

**Steps**:
1. `src/pages/franchises/FranchiseListPage.vue`: simple list with name + movie count.
2. `src/pages/franchises/FranchiseFormPage.vue`:
   - Fields: `name` (required), `description`
   - Movie order section (edit mode): draggable list of movies in this franchise
     - Use `vuedraggable` (install: `npm install vuedraggable@next`)
     - Each row: drag handle + poster thumbnail + title + year
     - On reorder: update local `franchise_order` values (1-based)
     - "保存排序" button → `PUT /admin/franchises/:id/movie-order` with `[{movie_id, franchise_order}]`
3. Add movie to franchise: search input → select movie → append to list with next order number.

**Files**:
- `admin/src/pages/franchises/FranchiseListPage.vue`
- `admin/src/pages/franchises/FranchiseFormPage.vue`

**Validation**:
- [ ] Dragging movies reorders them; "保存排序" persists new order
- [ ] Adding movie to franchise appends it to the list
- [ ] Franchise form creates/updates correctly

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Season/episode nested tables complex to implement | Use TDesign expandable rows; keep episode form as simple inline inputs |
| Multiple photo uploads: parallel vs sequential | Upload in parallel (`Promise.all`); show progress per file |
| Drag-and-drop library compatibility with Vue 3 | Use `vuedraggable@next` (Vue 3 compatible); test with TDesign table |

## Review Guidance

- TV/Anime season management is nested within the series edit page (not a separate route)
- Person photos: full array replacement on save (not incremental)
- Franchise movie order: "保存排序" is a separate explicit action (not auto-save on drag)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
