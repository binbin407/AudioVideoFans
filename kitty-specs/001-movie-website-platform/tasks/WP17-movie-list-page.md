---
work_package_id: WP17
title: Movie List Page
lane: planned
dependencies:
- WP14
subtasks:
- T075
- T076
- T077
- T078
phase: Phase 4 - Frontend
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

# Work Package Prompt: WP17 – Movie List Page

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP17 --base WP15
```

---

## Objectives & Success Criteria

- `/movies` renders a filterable, paginated grid of movie cards
- Filter sidebar: genre (multi-select), region (single), language (single), year range, sort order
- All filter state synced to URL query params via `useFilters`
- Changing any filter resets to page 1 and fetches new results
- `<title>` set to "电影列表 - 影视网" (or "动作电影 - 影视网" when genre filtered)

## Context & Constraints

- **Spec**: Scenario 2 (movie list), FR-4 (filters), FR-5 (sort), FR-6 (pagination)
- API: `GET /api/v1/movies?genre=&region=&language=&year_from=&year_to=&sort=&page=&page_size=24`
- Grid: 6 columns on desktop (`grid-cols-6`), 4 on tablet, 2 on mobile
- Filter sidebar collapses to a filter button + drawer on mobile
- Genre options fetched from `GET /api/v1/movies/filters` (or hardcoded from spec)

## Subtasks & Detailed Guidance

### Subtask T075 – Movie List API + Store

**Purpose**: Fetch movie list with filters; manage state.

**Steps**:
1. `src/api/movies.ts`:
   ```typescript
   export interface MovieListParams {
     genre?: string; region?: string; language?: string
     yearFrom?: number; yearTo?: number
     sort?: 'douban_score' | 'release_date' | 'popularity'
     page?: number; pageSize?: number
   }
   export const getMovies = (params: MovieListParams) =>
     client.get<PagedResponse<MediaCardDto>>('/api/v1/movies', { params })
   ```
2. `src/pages/MovieListPage.vue`:
   - Use `useFilters({ genre: '', region: '', language: '', sort: 'douban_score', page: 1 })`
   - `watch(filters, fetchMovies, { immediate: true })`
   - Store results in `movies` ref and `pagination` ref

**Files**:
- `frontend/src/api/movies.ts`
- `frontend/src/pages/MovieListPage.vue`

**Validation**:
- [ ] Page loads with default sort (douban_score DESC)
- [ ] URL updates when filters change
- [ ] Changing filter resets page to 1

---

### Subtask T076 – Filter Sidebar Component

**Purpose**: Filter controls for genre, region, language, year, sort.

**Steps**:
1. `src/components/FilterSidebar.vue`:
   - Props: `modelValue: FilterState`; emits `update:modelValue`
   - Genre: multi-select tag buttons (toggle on/off); genres list hardcoded from spec (动作/剧情/喜剧/爱情/科幻/恐怖/动画/纪录片/其他)
   - Region: single-select buttons (全部/中国大陆/香港/台湾/美国/英国/日本/韩国/其他)
   - Language: single-select (全部/普通话/粤语/英语/日语/韩语)
   - Sort: `<select>` dropdown (评分最高/最新上映/最热门)
   - Year range: two `<input type="number">` fields (from/to)
   - "重置筛选" button calls `reset()`
2. Mobile: hidden by default; shown in slide-over drawer triggered by filter button.

**Files**:
- `frontend/src/components/FilterSidebar.vue`

**Validation**:
- [ ] Selecting genre "动作" updates URL `?genre=action`
- [ ] Multi-select: can select multiple genres
- [ ] "重置筛选" clears all filters

---

### Subtask T077 – Movie Grid + Loading States

**Purpose**: Responsive grid of MediaCards with skeleton loading.

**Steps**:
1. In `MovieListPage.vue`:
   - Grid: `grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4`
   - Loading: show 24 skeleton cards (`bg-gray-700 animate-pulse rounded`)
   - Empty state: "没有找到符合条件的电影" with reset button
   - Each card: `<MediaCard>` with movie data
2. `src/components/SkeletonCard.vue`: grey placeholder matching MediaCard dimensions.

**Files**:
- `frontend/src/components/SkeletonCard.vue`

**Validation**:
- [ ] 24 skeleton cards shown during loading
- [ ] Empty state shown when API returns 0 results
- [ ] Grid is 6 columns on large screens

---

### Subtask T078 – Page Title + SEO Meta

**Purpose**: Dynamic page title based on active filters.

**Steps**:
1. Compute page title:
   ```typescript
   const pageTitle = computed(() => {
     if (filters.genre) return `${genreLabel(filters.genre)}电影`
     if (filters.region) return `${filters.region}电影`
     return '电影列表'
   })
   usePageMeta(pageTitle, computed(() => `浏览${pageTitle.value}，按评分、上映时间筛选`))
   ```
2. `genreLabel`: map API genre value to Chinese display name.

**Files**:
- Part of `frontend/src/pages/MovieListPage.vue`

**Validation**:
- [ ] Default: title is "电影列表 - 影视网"
- [ ] With genre=action: title is "动作电影 - 影视网"
- [ ] Meta description updates with title

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Filter watch triggers double fetch on init | Use `{ immediate: true }` only once; guard with `isFirstLoad` flag |
| Genre multi-select URL encoding | Use comma-separated string `?genre=action,comedy` or repeated params |
| Mobile filter drawer z-index conflicts | Use `z-50` for drawer overlay |

## Review Guidance

- Genre filter: multi-select (multiple genres can be active simultaneously)
- Sort default: `douban_score` DESC (highest rated first)
- Page resets to 1 on any filter change (not just page param change)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
