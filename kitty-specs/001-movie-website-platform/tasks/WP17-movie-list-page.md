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

## Design Reference

**Source**: `design/design.pen` — frame `ceslO` (Movie List - 电影列表页), width 1440px

### Page Layout

```
layout: vertical
fill: #0B0B0E
```

Content area (`1JmdX`):
```
padding: [32, 60, 40, 60]
layout: vertical
gap: 24
```

### Breadcrumb

```
gap: 8px
fontSize: 13
```
- "首页": `color: #6B6B70`
- "/": `color: #4A4A50`
- "电影": `color: #FFFFFF`

### Page Title

```
fontSize: 32
fontWeight: 700
color: #FFFFFF
```

### Filter Panel (frame `m5Vnm`)

```
cornerRadius: 12
fill: #16161A
border: 1px solid #2A2A2E
padding: 24
layout: vertical
gap: 16
width: fill_container
```

Filter rows (each `width: fill_container`, `alignItems: center`, `gap: 12`):
1. **Genre Filter** — multi-select tag chips
2. **Region Filter** — single-select chips (全部/中国大陆/香港/台湾/美国/英国/日本/韩国/其他)
3. **Language Filter** — single-select chips (全部/普通话/粤语/英语/日语/韩语)
4. **Year Filter** — range inputs or chip groups
5. **Score Filter** — score range chips (全部/9分以上/8分以上/7分以上/6分以下)

Filter chip style:
- Default: `cornerRadius: 6`, `padding: [4, 12]`, `fill: transparent`, `border: 1px solid #2A2A2E`, `color: #B8B9B6`, `fontSize: 13`
- Active: `fill: #FF840020`, `border: 1px solid #FF8400`, `color: #FF8400`

### Sort Bar (frame `hfo8Q`)

```
justify-content: space-between
width: fill_container
alignItems: center
```

- Left (`UUAM6`): sort option buttons, `gap: 16px`
  - Active sort: `color: #FFFFFF, fontWeight: 600`
  - Inactive: `color: #6B6B70`
- Right (`MRdif`): "共 2,486 部", `fontSize: 13, color: #6B6B70`

### Movie Grid (frame `dhZGJ`)

```
display: grid
grid-template-columns: repeat(6, 1fr)
gap: 16px
width: fill_container
```

Each card (`yCTX4` etc.): `layout: vertical`, `gap: 10`, `width: fill_container`

### Pagination (frame `vm6BO`)

```
justify-content: center
padding: [24, 0]
gap: 8px
```

Buttons: `36×36px`, `cornerRadius: 8`
- Active: `fill: #FF8400`, `color: #FFFFFF`
- Inactive: `border: 1px solid #2A2A2E`, `color: #FFFFFF`
- Prev/Next: same border style, chevron icons

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
