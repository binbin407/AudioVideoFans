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
