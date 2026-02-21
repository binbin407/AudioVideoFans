---
work_package_id: WP18
title: Movie Detail Page
lane: planned
dependencies:
- WP14
subtasks:
- T079
- T080
- T081
- T082
- T083
- T126
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

# Work Package Prompt: WP18 – Movie Detail Page

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP18 --base WP15
```

---

## Objectives & Success Criteria

- `/movies/:id` renders full movie detail: hero backdrop, poster, metadata, synopsis, credits, videos, gallery, similar
- Synopsis collapses to 3 lines with "展开" toggle when text > 200 chars
- Credits section: directors row + cast grid (max 20, "查看全部" if more)
- Video section: YouTube/Bilibili embed thumbnails; click opens modal player
- Image gallery: thumbnail grid; click opens lightbox
- Similar movies: horizontal scroll row of 6 MediaCards

## Context & Constraints

- **Spec**: Scenario 3 (movie detail), FR-7 (synopsis fold), FR-8 (credits), FR-9 (videos), FR-10 (gallery)
- Hero: backdrop image full-width with dark overlay; poster floated left on desktop
- Franchise badge: if `franchise_id` set, show franchise name linking to `/franchises/{id}`
- Release dates: show all regions (e.g., "中国大陆: 2024-03-15 / 北美: 2024-03-01")
- `<title>`: `{titleCn}({year}) - 影视网`

## Design Reference

**Source**: `design/design.pen` — frame `cNAF4` (Movie Detail - 电影详情页), width 1440px

### Detail Hero (frame `xlTB6`)

```
height: 480px
layout: none (absolute positioning)
clip: true
width: fill_container
```

Layers (bottom to top):
1. **Backdrop** (`YvoO2`): `1440×480px`, `object-cover`
2. **Bottom gradient** (`Blcn1`): `linear-gradient(0deg, #0B0B0EFF 65%, #0B0B0E00 100%)`
3. **Left gradient** (`vKYgq`): `linear-gradient(90deg, #0B0B0EBB 0%, #0B0B0E00 50%)`
4. **Blur overlay** (`Qx85d`): `#0B0B0E66` (semi-transparent dark)
5. **Movie Info** (`ZztYP`): positioned at `x:60, y:100`, `width: 1320px`, `alignItems: end`, `gap: 40px`
   - **Poster** (`0VCaJ`): `240×340px`, `cornerRadius: 12`, `box-shadow: 0 8px 20px #00000066`
   - **Info Column** (`8yZxp`): `width: fill_container`, `layout: vertical`, `gap: 16px`

### Info Column Contents

- Genre tag chip: `cornerRadius: 4`, `padding: [3, 8]`, `fontSize: 12`, `color: #FF8400`, `border: 1px solid #FF8400`
- Title (CN): `fontSize: 36, fontWeight: 700, color: #FFFFFF`
- Original title: `fontSize: 16, color: #B8B9B6`
- Meta row: year + rating + duration + region, `gap: 16px`, `fontSize: 14, color: #B8B9B6`
- Score badges row: Douban score `color: #22c55e` (large), IMDB score
- Action buttons: "加入片单" + "分享" buttons

### Main Content (frame `H7xsg`)

```
padding: [32, 60, 40, 60]
layout: vertical
gap: 40
```

### Rating Cards (frame `7I1xr`, gap 32)

- **Douban** (`XBxxb`): `cornerRadius: 12`, `fill: #16161A`, `border: 1px solid #2A2A2E`, `padding: 24`, `layout: vertical`, `gap: 16`
- **IMDB** (`QT1ay`): `width: 200px`, `cornerRadius: 12`, `fill: #16161A`, `padding: 24`, `alignItems: center`, `justifyContent: center`
- **Mtime** (`57z14`): same as Douban

### Synopsis Section (frame `aeD5N`)

```
layout: vertical
gap: 16
```
- Title: `fontSize: 20, fontWeight: 700, color: #FFFFFF`
- Text: `fontSize: 14, color: #B8B9B6, lineHeight: 1.8, width: fill_container`
- "展开全文 ▼": `fontSize: 13, fontWeight: 500, color: #FF8400`

### Cast Section (frame `rAf6w`)

- Section header: title `fontSize: 20, fontWeight: 700` + "查看全部" link, `justify-content: space-between`
- Cast grid (`UH8nm`): `gap: 16px`; each card: avatar (80×110px) + name + character

### Image Gallery (frame `xQjSD`)

- Grid (`sGqYA`): `gap: 12px`; thumbnails `cornerRadius: 8`
- "查看全部图片 →": `fontSize: 13, fontWeight: 500, color: #FF8400`

### Franchise Section (frame `iKkg1`)

- Card (`IUJOU`): `cornerRadius: 12`, `fill: #16161A`, `border: 1px solid #2A2A2E`, `padding: 20`, `layout: vertical`, `gap: 16`

### Awards Section (frame `Rsz6D`)

- List (`Eohdr`): `cornerRadius: 12`, `fill: #16161A`, `border: 1px solid #2A2A2E`, `clip: true`
- Each row: `padding: [16, 20]`, `border-bottom: 1px solid #2A2A2E`

### Similar Content Section (frame `CaNAK`)

- Grid (`oQFwG`): `gap: 16px`, same 6-column grid as list page

## Subtasks & Detailed Guidance

### Subtask T079 – Movie Detail API + Page Shell

**Purpose**: Fetch movie detail and render page shell with hero section.

**Steps**:
1. `src/api/movies.ts` — add:
   ```typescript
   export const getMovieDetail = (id: number) =>
     client.get<MovieDetailDto>(`/api/v1/movies/${id}`)
   ```
2. `src/pages/MovieDetailPage.vue`:
   - `onMounted`: fetch by `route.params.id`; handle 404 (redirect to NotFoundPage)
   - Hero section: backdrop image (`object-cover w-full h-64 md:h-96`) with `linear-gradient` overlay
   - Poster: `w-32 md:w-48` floated left, `rounded-lg shadow-xl`
   - Title block: `titleCn` (h1), `titleOriginal` (subtitle), year, genres (tag pills), runtime, region
   - `usePageMeta(computed(() => movie.value ? \`\${movie.value.titleCn}(\${movie.value.year})\` : '加载中'))`

**Files**:
- `frontend/src/pages/MovieDetailPage.vue`

**Validation**:
- [ ] Page renders hero backdrop + poster + title block
- [ ] 404 movie ID redirects to not-found page
- [ ] `<title>` set to `{titleCn}(2024) - 影视网`

---

### Subtask T080 – Synopsis + Metadata Section

**Purpose**: Collapsible synopsis and full metadata display.

**Steps**:
1. Synopsis collapse:
   ```vue
   <div :class="expanded ? '' : 'line-clamp-3'" ref="synopsisEl">{{ movie.synopsis }}</div>
   <button v-if="isTruncated" @click="expanded = !expanded">
     {{ expanded ? '收起' : '展开' }}
   </button>
   ```
   Detect truncation: compare `scrollHeight > clientHeight` after mount.
2. Metadata grid: douban_score (large, colored), imdb_score, release_dates (all regions), duration, language, production_companies, keywords (tag links to `/movies?keyword=`).
3. Franchise badge: `<RouterLink v-if="movie.franchiseId" :to="\`/franchises/\${movie.franchiseId}\`">{{ movie.franchiseName }}</RouterLink>`.

**Files**:
- Part of `frontend/src/pages/MovieDetailPage.vue`

**Validation**:
- [ ] Synopsis > 200 chars shows "展开" button; click expands
- [ ] All release dates shown (multiple regions)
- [ ] Franchise badge links to franchise page

---

### Subtask T081 – Credits Section

**Purpose**: Directors and cast display with overflow handling.

**Steps**:
1. Directors row: horizontal list of avatar + name cards (max 5 shown).
2. Cast grid: `grid grid-cols-3 md:grid-cols-5 gap-3`; each card: avatar (60×80px) + name + character name.
3. Show max 10 cast members; "查看全部演员" button if `cast.length > 10` (links to same page with `#cast` anchor showing all).
4. Person avatar: `<LazyImage>` with `cosUrl(person.avatarCosKey)`; fallback to grey silhouette SVG.
5. Each person name links to `/people/{personId}`.

**Files**:
- Part of `frontend/src/pages/MovieDetailPage.vue`

**Validation**:
- [ ] Directors shown as horizontal row
- [ ] Cast grid shows max 10; "查看全部" shown if more
- [ ] Person names link to person detail page

---

### Subtask T082 – Videos + Image Gallery

**Purpose**: Video thumbnails with modal player; image gallery with lightbox.

**Steps**:
1. Videos section:
   - Horizontal scroll row of video thumbnails (16:9 aspect ratio)
   - Each thumbnail: `<img>` from video `thumbnail_url` + play icon overlay
   - Click: open `VideoModal.vue` with iframe embed (`youtube.com/embed/{key}` or `player.bilibili.com/player.html?bvid={key}`)
2. `src/components/VideoModal.vue`: full-screen overlay with iframe + close button (Escape key closes).
3. Image gallery:
   - Grid of thumbnail images (`grid-cols-4 md:grid-cols-6`)
   - Click: open `ImageLightbox.vue` with full-size image + prev/next navigation
4. `src/components/ImageLightbox.vue`: overlay with full image, prev/next arrows, close on Escape or backdrop click.

**Files**:
- `frontend/src/components/VideoModal.vue`
- `frontend/src/components/ImageLightbox.vue`

**Validation**:
- [ ] Clicking video thumbnail opens modal with embed
- [ ] Escape key closes modal
- [ ] Gallery lightbox prev/next navigates between images

---

### Subtask T083 – Similar Movies Section

**Purpose**: "相似电影" horizontal scroll row.

**Steps**:
1. Fetch `GET /api/v1/movies/{id}/similar` on page load (parallel with main detail fetch).
2. Render as `<ContentRow title="相似电影" :items="similar" viewAllLink="" />` (no "查看全部" for similar).
3. If similar returns empty array, hide the section entirely.

**Files**:
- Part of `frontend/src/pages/MovieDetailPage.vue`

**Validation**:
- [ ] Similar section shows ≤6 cards
- [ ] Section hidden when no similar movies
- [ ] Similar fetched in parallel with main detail (not sequential)

---

### Subtask T126 – Full Credits Page (`/movies/:id/credits`)

**Purpose**: Implement the "全部演职员" page referenced by FR-2, reachable via the "查看全部演职员" link on the detail page.

**Steps**:
1. `frontend/src/pages/movies/[id]/credits.vue` (route: `/movies/:id/credits`):
   - Fetch `GET /api/v1/movies/:id/credits` on mount; response is credits grouped by department
   - Page header: breadcrumb — 电影名(link to `/movies/:id`) → 全部演职员
   - `<title>`: `{titleCn} 全部演职员 - 影视网`
   - Render one `<section>` per department: 导演 / 编剧 / 主演 / 制片人 / 其他
     - Department heading: `fontSize: 18, fontWeight: 700, color: #FFFFFF`
     - Each credit row: avatar (`60×60px` round, `cornerRadius: 50%`) + name_cn + character_name (italic, `color: #6B6B70`)
     - Avatar links to `/people/:id`; name links to `/people/:id`
     - Placeholder avatar when `avatar_cos_key` is null (grey circle)
2. If a department has zero credits, omit that section entirely (no empty heading).
3. Reuse `cosUrl()` helper for avatar images; `loading="lazy"` on all avatars.
4. On mobile: single column; on desktop: 2-column grid within each department.

**Files**:
- `frontend/src/pages/movies/[id]/credits.vue`

**Validation**:
- [ ] Page accessible via `/movies/123/credits`
- [ ] Breadcrumb links back to `/movies/123`
- [ ] `<title>` contains movie name + "全部演职员"
- [ ] Departments with no credits are hidden
- [ ] Avatar click navigates to `/people/:id`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Synopsis truncation detection unreliable before fonts load | Use `nextTick` + `ResizeObserver` to re-check after layout |
| YouTube embeds blocked in China | Show Bilibili embed when `video_site='bilibili'`; YouTube as fallback |
| Lightbox keyboard navigation conflicts with page scroll | `preventDefault` on arrow keys when lightbox is open |

## Review Guidance

- Synopsis: collapse to 3 lines (not 200 chars) — use CSS `line-clamp-3`; detect overflow via scrollHeight
- Credits: person avatars link to `/people/{id}`; character name shown below actor name
- Videos: modal iframe; do NOT autoplay on thumbnail hover

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
