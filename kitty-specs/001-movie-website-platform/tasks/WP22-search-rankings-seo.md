---
work_package_id: WP22
title: Search, Rankings + SEO
lane: planned
dependencies:
- WP14
- WP15
subtasks:
- T095
- T096
- T097
- T098
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

# Work Package Prompt: WP22 – Search, Rankings + SEO

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP22 --base WP15
```

---

## Objectives & Success Criteria

- `/search?q=` shows paginated results with type-filter tabs and per-type counts
- `/rankings` shows 7 ranking lists in tabs (热门电影/热门剧集/热门动漫/高分电影/高分剧集/高分动漫/电影Top100)
- Rankings 1-3 show gold/silver/bronze badges
- `sitemap.xml` generated at build time listing all static routes
- `robots.txt` served from `/public`

## Context & Constraints

- **Spec**: Scenario 4 (search), FR-18/19 (search), FR-20/21 (rankings), FR-43/44/45/46
- Search results: type tabs show count badges; switching tab filters displayed results (client-side from same API response)
- Rankings: all 7 lists fetched in one `GET /api/v1/rankings` call; tabs switch displayed list
- SEO: `<meta name="robots" content="index,follow">` on all public pages; noindex on admin
- `sitemap.xml`: static routes only (dynamic content pages not included — too many)

## Design Reference

**Source**: `design/design.pen` — frames `jQSVK` (Search Results), `Qh2Ss` (Rankings)

### Search Results Page (frame `jQSVK`)

**Search Bar** (top of page, frame `ivRXO`):
```
height: 52px
background: #16161A
cornerRadius: 12
padding: [0, 20]
border: 2px solid #FF8400   ← active state
gap: 12px
width: fill_container
```
- Search icon: `#FF8400`, 22px (Material Symbols Rounded `search`)
- Input: `fontSize: 16, color: #FFFFFF, placeholder-color: #4A4A50`
- Clear button: `#6B6B70`, 20px (Material Symbols Rounded `close`)

**Type Filter Tabs** (below search bar):
```
gap: 0
border-bottom: 1px solid #2A2A2E
```
- Tab: `padding: [12, 20]`, `fontSize: 14`
- Active: `color: #FFFFFF`, `border-bottom: 2px solid #FF8400`, `fontWeight: 600`
- Inactive: `color: #6B6B70`
- Count badge: `cornerRadius: 10`, `fill: #2A2A2E`, `padding: [2, 8]`, `fontSize: 11, color: #B8B9B6`
- Tabs: 全部 / 电影 / 电视剧 / 动漫 / 影人

**Result Items**:
```
layout: vertical
gap: 0
```
Each item: `padding: [20, 0]`, `border-bottom: 1px solid #2A2A2E`, `gap: 16px`, `layout: horizontal`
- Poster/avatar: `60×90px` (media) or `60×60px` round (person), `cornerRadius: 8`
- Info: title `fontSize: 16, fontWeight: 600, color: #FFFFFF` + meta `fontSize: 13, color: #6B6B70`
- Score badge: right-aligned, `color: #22c55e` (≥8.0) / `#eab308` (≥6.0)

**Empty State**:
- Icon: `#4A4A50`, 48px
- Text: "没有找到相关内容", `fontSize: 16, color: #6B6B70`
- Suggestion: `fontSize: 14, color: #4A4A50`

### Rankings Page (frame `Qh2Ss`)

**Category Tabs** (horizontal scroll):
```
gap: 8px
padding: [0, 60]
border-bottom: 1px solid #2A2A2E
overflow-x: auto
```
- Tab chip: `cornerRadius: 20`, `padding: [8, 20]`, `fontSize: 14`
- Active: `fill: #FF8400`, `color: #FFFFFF`
- Inactive: `fill: #16161A`, `border: 1px solid #2A2A2E`, `color: #B8B9B6`
- Tabs: 热门电影 / 热门剧集 / 热门动漫 / 高分电影 / 高分剧集 / 高分动漫 / 电影Top100

**Rank List**:
```
layout: vertical
gap: 0
cornerRadius: 12
fill: #16161A
border: 1px solid #2A2A2E
clip: true
```
Each rank row: `padding: [16, 20]`, `border-bottom: 1px solid #2A2A2E`, `gap: 16px`
- Rank number: `width: 32px`, `fontSize: 20, fontWeight: 700`
  - Rank 1: `color: #FFD700`, row `background: #FFD70008`
  - Rank 2: `color: #C0C0C0`, row `background: #C0C0C008`
  - Rank 3: `color: #CD7F32`, row `background: #CD7F3208`
  - Rank 4+: `color: #4A4A50`
- Poster: `48×72px`, `cornerRadius: 6`
- Title: `fontSize: 16, fontWeight: 600, color: #FFFFFF`
- Meta: year + genre, `fontSize: 13, color: #6B6B70`
- Score: right-aligned, `fontSize: 18, fontWeight: 700, color: #22c55e`

## Subtasks & Detailed Guidance

### Subtask T095 – Search Results Page

**Purpose**: Full-text search results with type tabs and pagination.

**Steps**:
1. `src/pages/SearchPage.vue`:
   - Read `q` from `route.query.q`; watch for changes and re-fetch
   - Fetch `GET /api/v1/search?q={q}&type={activeType}&page={page}`
   - Type tabs: 全部 / 电影({count}) / 剧集({count}) / 动漫({count}) / 人物({count})
   - Switching tab: update `activeType` → re-fetch with `type=` param; reset to page 1
   - Result card: poster thumbnail (40×60px) + title + year + content type badge + synopsis snippet (60 chars)
   - Empty state: "没有找到「{q}」的相关结果"
2. `src/api/search.ts` — add:
   ```typescript
   export const search = (q: string, type?: string, page = 1) =>
     client.get<SearchResponse>('/api/v1/search', { params: { q, type, page } })
   ```
3. `usePageMeta(computed(() => \`搜索「\${q}」\`))`.

**Files**:
- `frontend/src/pages/SearchPage.vue`

**Validation**:
- [ ] `?q=星际` shows results with type counts in tab badges
- [ ] Clicking "电影" tab re-fetches with `type=movie`
- [ ] Empty query shows empty state (not error)
- [ ] Pagination works across search results

---

### Subtask T096 – Rankings Page

**Purpose**: All 7 ranking lists in a tabbed interface.

**Steps**:
1. `src/pages/RankingsPage.vue`:
   - Fetch `GET /api/v1/rankings` once on mount; store all 7 lists
   - Tab bar: 热门电影 / 热门剧集 / 热门动漫 / 高分电影 / 高分剧集 / 高分动漫 / 电影Top100
   - Active tab stored in URL: `?tab=hot_movies` (default)
   - Ranking list: vertical list; each item:
     - Rank badge: 1=gold circle, 2=silver circle, 3=bronze circle, 4+=grey number
     - Poster thumbnail (40×60px) + title + year + score
     - Item links to `/{contentType}/{id}`
2. Top100 note: show gate criteria "豆瓣评分≥7.0 且 评价人数≥1000" as subtitle.
3. `usePageMeta('排行榜')`.

**Files**:
- `frontend/src/pages/RankingsPage.vue`

**Validation**:
- [ ] All 7 tabs present; switching tab shows correct list
- [ ] Rank 1-3 have gold/silver/bronze badges
- [ ] Top100 tab shows only movies meeting gate criteria
- [ ] Tab selection persisted in URL `?tab=`

---

### Subtask T097 – SEO Meta Tags

**Purpose**: Add canonical URLs, Open Graph tags, and structured data to key pages.

**Steps**:
1. Update `usePageMeta` composable to also set:
   - `<meta property="og:title">`
   - `<meta property="og:description">`
   - `<meta property="og:image">` (poster COS URL for detail pages)
   - `<link rel="canonical">` (current URL without query params for list pages)
2. Movie/TV/Anime detail pages: pass `posterCosKey` to `usePageMeta` for OG image.
3. List pages: canonical = `/movies`, `/tv`, `/anime` (strip filter params).
4. Add `<meta name="robots" content="index,follow">` to `index.html`.
5. `public/robots.txt`:
   ```
   User-agent: *
   Allow: /
   Disallow: /admin/
   Sitemap: https://yourdomain.com/sitemap.xml
   ```

**Files**:
- `frontend/src/composables/usePageMeta.ts` (extend with OG tags)
- `frontend/public/robots.txt`
- `frontend/index.html`

**Validation**:
- [ ] Movie detail page has `og:image` set to poster URL
- [ ] List pages have canonical URL without filter params
- [ ] `/robots.txt` accessible and disallows `/admin/`

---

### Subtask T098 – Sitemap Generation

**Purpose**: Static sitemap.xml for search engine indexing.

**Steps**:
1. `frontend/scripts/generate-sitemap.ts` (run at build time):
   ```typescript
   const staticRoutes = ['/', '/movies', '/tv', '/anime', '/rankings', '/search',
     '/awards/oscar', '/awards/golden-globe', '/awards/cannes',
     '/awards/venice', '/awards/berlin', '/awards/hkfa', '/awards/golden-horse']
   const sitemap = staticRoutes.map(r =>
     `<url><loc>https://yourdomain.com${r}</loc></url>`).join('\n')
   fs.writeFileSync('public/sitemap.xml', `<?xml version="1.0"?><urlset xmlns="...">\n${sitemap}\n</urlset>`)
   ```
2. Add to `package.json` scripts: `"build": "npm run sitemap && vite build"`.
3. Dynamic content pages (individual movies/people) excluded from sitemap (too many; rely on internal links for crawling).

**Files**:
- `frontend/scripts/generate-sitemap.ts`
- `frontend/package.json` (update build script)

**Validation**:
- [ ] `npm run build` generates `public/sitemap.xml`
- [ ] Sitemap contains all 7 award event URLs
- [ ] Sitemap is valid XML

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Search re-fetch on every tab switch (wasteful) | Cache full response; filter client-side by type for tab switching; only re-fetch on `q` change |
| Rankings payload large (7 lists × 50-100 items) | Single fetch on mount; no re-fetch on tab switch |
| OG image URL requires absolute URL | Use `VITE_COS_BASE_URL` env var to construct absolute URL |

## Review Guidance

- Search tabs: type counts come from `typeCounts` in API response (not re-counted client-side)
- Rankings: single API call; tab switching is client-side only
- SEO: canonical URLs strip filter params (important for avoiding duplicate content penalties)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
