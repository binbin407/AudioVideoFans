---
work_package_id: WP20
title: Anime Pages
lane: planned
dependencies:
- WP14
- WP15
- WP19
subtasks:
- T088
- T089
- T090
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

# Work Package Prompt: WP20 – Anime Pages

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP20 --base WP15
```

---

## Objectives & Success Criteria

- `/anime` list page has origin tabs (全部/国漫/日漫) and source_material filter row
- `/anime/:id` detail page shows voice actors section separate from regular cast
- `/anime/:id/seasons/:n` season detail page (reuses SeasonDetailPage structure)

## Context & Constraints

- **Spec**: Scenarios 10 (anime list) and 11 (anime detail), FR-15 (origin tabs), FR-35 (source_material filter)
- Origin tabs are mutually exclusive (not multi-select): 全部/国漫(`cn`)/日漫(`jp`)
- Source material filter: 全部/原创/漫画改编/小说改编/游戏改编 (single-select)
- Voice actors section: separate from cast; shows character name prominently

## Design Reference

**Source**: `design/design.pen` — frames `WOq8S` (Anime List), `zJJHL` (Anime Detail)

### Anime List (frame `WOq8S`)

Same layout as Movie List with these differences:

**Filter Panel** (`fC0iu`) — 7 filter rows (most of any list page):
1. Genre Filter
2. **Origin Filter** — tabs: 全部 / 国漫 / 日漫 (mutually exclusive, styled as tab buttons)
3. **Source Filter** — 全部/原创/漫画改编/小说改编/游戏改编
4. Region Filter
5. Language Filter
6. Year Filter
7. Score Filter

Grid (`Wi5SH`): 6 columns, `gap: 16px`

### Anime Detail (frame `zJJHL`)

**Detail Hero** (`zxYuj`): identical structure to TV series detail hero
- Height: `480px`, same gradient layers
- Poster: `240×340px`, `cornerRadius: 12`, shadow

**Main Content** (`swcJY`): `padding: [32, 60, 40, 60]`, `layout: vertical`, `gap: 40`

**Ratings** (`P3Y29`): Douban + IMDB (same as TV, no Mtime)

**Synopsis** (`aJ1BO`): `fontSize: 14, color: #B8B9B6, lineHeight: 1.8`

**Seasons Section** (`KMfam`): same accordion structure as TV series

**Voice Cast Section** (`T7VCK`):
- Label: "声优" (not "演员")
- Cast grid (`ahjYG`): `gap: 16px`
- Each card: voice actor avatar + name + character name (character name more prominent than actor name)
- Character name: `fontSize: 14, fontWeight: 600, color: #FFFFFF`
- Actor name: `fontSize: 12, color: #B8B9B6`

**Similar Content** (`GddaQ`): same pattern as other detail pages

### Anime Season Detail

Reuses the same `SeasonDetailPage` component as TV series (same design frame `m8Owi`):
- Breadcrumb: 首页 / 动漫 / {动漫名} / 第N季
- Episode list: same structure, episode code `color: #FF8400`
- Anime season detail: same structure as TV season detail but fetches from `/anime/:id/seasons/:n`

## Subtasks & Detailed Guidance

### Subtask T088 – Anime List Page

**Purpose**: Anime list with origin tabs and source_material filter.

**Steps**:
1. `src/pages/AnimeListPage.vue`: mirrors TvListPage structure.
2. `src/api/anime.ts`:
   ```typescript
   export const getAnimeList = (params: AnimeListParams) =>
     client.get<PagedResponse<AnimeMediaCardDto>>('/api/v1/anime', { params })
   ```
3. Origin tabs above the grid (not in sidebar):
   ```vue
   <div class="flex gap-2 mb-4">
     <button v-for="tab in originTabs" :key="tab.value"
       :class="filters.origin === tab.value ? 'bg-red-600' : 'bg-gray-700'"
       @click="filters.origin = tab.value">
       {{ tab.label }}
     </button>
   </div>
   ```
   Tabs: `[{value:'', label:'全部'}, {value:'cn', label:'国漫'}, {value:'jp', label:'日漫'}]`
4. Source material filter row (below tabs, above grid): single-select pill buttons.
5. `AnimeMediaCardDto` card badge: show `originLabel` (国漫/日漫/其他).
6. `usePageMeta(computed(() => filters.origin === 'cn' ? '国漫' : filters.origin === 'jp' ? '日漫' : '动漫列表'))`.

**Files**:
- `frontend/src/pages/AnimeListPage.vue`
- `frontend/src/api/anime.ts`

**Validation**:
- [ ] Clicking "国漫" tab filters to `origin=cn`; URL updates
- [ ] Source material filter works independently of origin tab
- [ ] Card badge shows origin label

---

### Subtask T089 – Anime Detail Page

**Purpose**: Anime detail with voice actors section and anime-specific metadata.

**Steps**:
1. `src/pages/AnimeDetailPage.vue`: extends movie detail pattern with:
   - Extra metadata: `studio` (制作公司), `sourceMaterial` label, `originLabel`
   - Voice actors section (after regular cast):
     ```vue
     <section v-if="anime.voiceActors?.length">
       <h2>配音演员</h2>
       <div class="grid grid-cols-3 md:grid-cols-5 gap-3">
         <div v-for="va in anime.voiceActors" :key="va.personId">
           <LazyImage :src="cosUrl(va.avatarCosKey)" />
           <p class="font-medium">{{ va.nameCn }}</p>
           <p class="text-sm text-gray-400">{{ va.characterName }}</p>
         </div>
       </div>
     </section>
     ```
   - Seasons accordion: same `<SeasonsAccordion>` component but links to `/anime/:id/seasons/:n`
2. `src/api/anime.ts` — add `getAnimeDetail(id)`.

**Files**:
- `frontend/src/pages/AnimeDetailPage.vue`

**Validation**:
- [ ] Voice actors section separate from cast section
- [ ] Each voice actor shows character name
- [ ] Studio and source material shown in metadata
- [ ] Seasons accordion links to `/anime/:id/seasons/:n`

---

### Subtask T090 – Anime Season Detail Page

**Purpose**: Season detail for anime (same structure as TV, different API endpoint).

**Steps**:
1. `src/pages/AnimeSeasonDetailPage.vue`:
   - Route: `/anime/:id/seasons/:n`
   - Fetch `GET /api/v1/anime/:id/seasons/:n`
   - Identical rendering to `SeasonDetailPage.vue` (TV)
   - Breadcrumb: 首页 > 动漫 > {seriesTitleCn} > 第{n}季
   - Prev/next season links: `/anime/:id/seasons/{prevN}` and `/anime/:id/seasons/{nextN}`
2. Consider extracting shared `SeasonDetail` composable used by both TV and anime season pages to avoid duplication.

**Files**:
- `frontend/src/pages/AnimeSeasonDetailPage.vue`

**Validation**:
- [ ] Fetches from `/api/v1/anime/:id/seasons/:n` (not `/tv/`)
- [ ] Breadcrumb shows "动漫" not "剧集"
- [ ] Prev/next links use `/anime/` prefix

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Origin tab + source_material filter interaction | Both are independent filters; both sent as query params simultaneously |
| SeasonDetailPage duplication between TV and anime | Extract `useSeasonDetail(contentType, id, n)` composable |
| Voice actors section empty for older anime entries | Hide section with `v-if="anime.voiceActors?.length"` |

## Review Guidance

- Origin tabs: mutually exclusive (selecting 国漫 deselects 日漫); "全部" clears origin filter
- Voice actors: character name is the primary label (larger/bolder than actor name)
- Anime season detail: must use `/anime/` API path, not `/tv/` path

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
