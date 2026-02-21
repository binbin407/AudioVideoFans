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
