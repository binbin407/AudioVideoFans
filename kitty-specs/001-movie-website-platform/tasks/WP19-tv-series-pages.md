---
work_package_id: WP19
title: TV Series Pages
lane: planned
dependencies:
- WP14
subtasks:
- T084
- T085
- T086
- T087
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

# Work Package Prompt: WP19 – TV Series Pages

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP19 --base WP15
```

---

## Objectives & Success Criteria

- `/tv` list page with `air_status` multi-select filter (连载中/已完结/待播) in addition to standard filters
- `/tv/:id` detail page shows seasons accordion; `next_episode_info` banner when airing
- `/tv/:id/seasons/:n` season detail page shows full episode list with S01E01 codes
- Season detail has prev/next season navigation links

## Context & Constraints

- **Spec**: Scenarios 8 (TV list) and 9 (TV detail), FR-10 (season/episode hierarchy)
- TV list card shows `air_status` badge (连载中=green, 已完结=grey, 待播=blue)
- Seasons accordion: collapsed by default; click to expand shows season overview + episode count
- Season detail: episodes in a table/list with episode number, title, air date, duration

## Design Reference

**Source**: `design/design.pen` — frames `WWclq` (TV Series List), `tqMcW` (TV Series Detail)

### TV Series List (frame `WWclq`)

Same layout as Movie List (`ceslO`) with these differences:

**Filter Panel** (`7P5Zw`) — 6 filter rows:
1. Genre Filter
2. **Air Status Filter** (extra vs movies) — chips: 全部 / 连载中 / 已完结 / 待播
   - 连载中: `color: #22c55e`, `border-color: #22c55e`
   - 已完结: `color: #6B6B70`, `border-color: #2A2A2E`
   - 待播: `color: #3b82f6`, `border-color: #3b82f6`
3. Region Filter
4. Language Filter
5. Year Filter
6. Score Filter

Grid (`euyzF`): 6 columns, `gap: 16px` — same as movie grid

### TV Series Detail (frame `tqMcW`)

**Detail Hero** (`bF5Pa`): identical structure to movie detail hero
- Height: `480px`, backdrop + bottom gradient + left gradient + overlay
- Poster: `240×340px`, `cornerRadius: 12`, shadow
- Info column: `layout: vertical`, `gap: 16px`

**Main Content** (`pmWCU`): `padding: [32, 60, 40, 60]`, `layout: vertical`, `gap: 40`

**Ratings** (`exJIL`): Douban + IMDB cards, `gap: 32` (no Mtime for TV)

**Synopsis** (`xgbIR`): same as movie — `fontSize: 14, color: #B8B9B6, lineHeight: 1.8`

**Seasons Section** (`4jzqG`):
```
layout: vertical
gap: 16
```
- Header (`KEEVd`): "全 N 季" title + sort/filter, `justify-content: space-between`
- Season list (`nGlik`): `layout: vertical`, `gap: 12`
- Each season row: `cornerRadius: 8`, `fill: #16161A`, `border: 1px solid #2A2A2E`, `padding: [16, 20]`
  - Expanded state: shows season poster + synopsis + episode count
  - Collapsed: shows season number + title + episode count + air date range

**Cast Section** (`9S4WD`): same pattern as movie cast

**Similar Content** (`Sidhd`): same pattern as movie similar

### TV Season Detail (frame `m8Owi`)

- Breadcrumb: 4 levels — 首页 / 电视剧 / {剧名} / 第N季
- Season header: poster (160×220px) + season info column
- Episode list: `layout: vertical`, `gap: 0`, each row `border-bottom: 1px solid #2A2A2E`
  - Episode row: `padding: [16, 20]`, episode code (S01E01) `color: #FF8400`, title, air date, duration
- Prev/Next season nav: bottom of page, `justify-content: space-between`
- `next_episode_info` banner: yellow strip at top of detail page when `air_status='airing'`

## Subtasks & Detailed Guidance

### Subtask T084 – TV List Page

**Purpose**: TV series list with air_status multi-filter.

**Steps**:
1. `src/pages/TvListPage.vue`: mirrors `MovieListPage.vue` structure.
2. `src/api/tv.ts`:
   ```typescript
   export const getTvList = (params: TvListParams) =>
     client.get<PagedResponse<TvMediaCardDto>>('/api/v1/tv', { params })
   ```
3. Additional filter in `FilterSidebar`: air_status multi-select checkboxes:
   - 连载中 (`airing`), 已完结 (`ended`), 待播 (`upcoming`)
   - Pass as repeated params: `?status=airing&status=ended`
4. `TvMediaCardDto` extends `MediaCardDto` with `airStatus` field.
5. `MediaCard` badge: show `airStatus` label with color coding.
6. `usePageMeta('剧集列表')`.

**Files**:
- `frontend/src/pages/TvListPage.vue`
- `frontend/src/api/tv.ts`

**Validation**:
- [ ] `?status=airing` shows only airing series
- [ ] Multi-select: can filter airing + ended simultaneously
- [ ] Card badge shows air status with correct color

---

### Subtask T085 – TV Detail Page

**Purpose**: Full TV series detail with seasons accordion and next episode banner.

**Steps**:
1. `src/pages/TvDetailPage.vue`:
   - Fetch `GET /api/v1/tv/:id` + `GET /api/v1/tv/:id/similar` in parallel
   - Hero, metadata, synopsis, credits sections: same pattern as MovieDetailPage
   - `next_episode_info` banner (when not null):
     ```vue
     <div v-if="tv.nextEpisodeInfo" class="bg-yellow-500 text-black px-4 py-2 rounded">
       下一集: S{{ pad(tv.nextEpisodeInfo.seasonNumber) }}E{{ pad(tv.nextEpisodeInfo.episodeNumber) }}
       「{{ tv.nextEpisodeInfo.title }}」 {{ tv.nextEpisodeInfo.airDate }}
     </div>
     ```
2. `usePageMeta(computed(() => tv.value?.titleCn ?? '加载中'))`.

**Files**:
- `frontend/src/pages/TvDetailPage.vue`

**Validation**:
- [ ] Airing series shows next episode banner
- [ ] Ended series has no next episode banner
- [ ] Similar TV series shown in horizontal row

---

### Subtask T086 – Seasons Accordion Component

**Purpose**: Collapsible seasons list with season overview.

**Steps**:
1. `src/components/SeasonsAccordion.vue`:
   - Props: `seasons: SeasonSummary[]`, `seriesId: number`
   - Each season row: season number + name + episode count + first air date
   - Click to expand: shows `overview` text + poster thumbnail + vote_average
   - "查看本季全部剧集 →" link to `/tv/{seriesId}/seasons/{seasonNumber}`
   - Only one season expanded at a time (accordion behavior)
2. Season poster: `<LazyImage>` with grey placeholder if `posterCosKey` is null.

**Files**:
- `frontend/src/components/SeasonsAccordion.vue`

**Validation**:
- [ ] Clicking season row expands it; clicking again collapses
- [ ] Only one season open at a time
- [ ] "查看本季全部剧集" links to correct season URL
- [ ] Season without poster shows grey placeholder

---

### Subtask T087 – Season Detail Page

**Purpose**: Full episode list for a specific season with prev/next navigation.

**Steps**:
1. `src/pages/SeasonDetailPage.vue`:
   - Route: `/tv/:id/seasons/:n`
   - Fetch `GET /api/v1/tv/:id/seasons/:n`
   - Breadcrumb: 首页 > 剧集 > {seriesTitleCn} > 第{n}季
   - Episode list: table with columns: 集数 (S01E01 format), 标题, 播出日期, 时长
   - Episode code: `S${String(season).padStart(2,'0')}E${String(ep).padStart(2,'0')}`
   - Still image thumbnail (40×22px) if `stillCosKey` present
   - Prev/next season navigation: `<RouterLink v-if="season.prevSeasonNumber">上一季</RouterLink>`
2. `usePageMeta(computed(() => \`\${season.value?.seriesTitleCn} 第\${n}季\`))`.

**Files**:
- `frontend/src/pages/SeasonDetailPage.vue`

**Validation**:
- [ ] Episodes shown with S01E01 format codes
- [ ] Prev season link absent for season 1
- [ ] Next season link absent for latest season
- [ ] Breadcrumb shows series title

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Accordion animation performance with many seasons | Use CSS `max-height` transition; avoid JS-based height animation |
| `air_status` multi-select URL encoding | Use repeated params `?status=airing&status=ended`; parse as array in `useFilters` |
| Season detail page reused for anime (different route) | Create separate `AnimeSeasonDetailPage.vue` in WP20 that reuses same component logic |

## Review Guidance

- TV list: air_status is multi-select (not single-select like region)
- Next episode banner: only shown when `nextEpisodeInfo` is non-null (not just when airing)
- Season accordion: one open at a time; "查看全部剧集" goes to season detail page

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
