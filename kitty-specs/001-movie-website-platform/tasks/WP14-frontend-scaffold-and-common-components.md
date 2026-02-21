---
work_package_id: WP14
title: Frontend Scaffold + Common Components
lane: planned
dependencies: []
subtasks:
- T060
- T061
- T062
- T063
- T064
- T065
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

# Work Package Prompt: WP14 – Frontend Scaffold + Common Components

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP14 --base WP03
```

---

## Objectives & Success Criteria

- Vue 3 + Vite + Tailwind CSS project initialized under `/frontend` with TypeScript
- Axios API client configured with base URL from env; typed response wrappers
- `MediaCard` component renders poster + title + score badge; used across all list pages
- `ScoreBar` component renders Douban score with color coding (green ≥8, yellow ≥6, red <6)
- `LazyImage` component with IntersectionObserver-based lazy loading + grey placeholder
- `Pagination` component with page number display and prev/next buttons

## Context & Constraints

- **Spec**: FR-1 (media card), FR-2 (score display), FR-6 (lazy image loading)
- Stack: Vue 3 Composition API + `<script setup>`, Vite 5, Tailwind CSS v3, TypeScript, Pinia, Vue Router 4
- COS image URLs: `https://{bucket}.cos.{region}.myqcloud.com/{cos_key}` — build helper `cosUrl(key)` in utils
- No UI component library for public frontend (custom Tailwind components only)
- API base URL from `VITE_API_BASE_URL` env variable

## Design Reference

**Source**: `design/design.pen` (all public frontend frames share this design system)

### Global Design Tokens

```css
/* Colors */
--color-bg:          #0B0B0E   /* page background */
--color-surface:     #16161A   /* cards, panels, filter boxes */
--color-surface-alt: #111114   /* admin sidebar */
--color-border:      #2A2A2E   /* all borders/dividers */
--color-accent:      #FF8400   /* primary orange — active states, CTAs, scores */
--color-text-primary:   #FFFFFF
--color-text-secondary: #B8B9B6
--color-text-muted:     #8E8E93
--color-text-label:     #6B6B70
--color-text-faint:     #4A4A50

/* Rank badge backgrounds (very subtle) */
--rank-gold:   #FFD70008
--rank-silver: #C0C0C008
--rank-bronze: #CD7F3208
```

```js
// tailwind.config.js — extend colors
colors: {
  bg: '#0B0B0E',
  surface: '#16161A',
  border: '#2A2A2E',
  accent: '#FF8400',
}
```

### Typography

- **Font family**: `Geist` (load via CDN or local — all design frames use Geist)
  ```html
  <!-- index.html -->
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=Geist:wght@400;500;600;700&display=swap" rel="stylesheet">
  ```
- **Base font**: `font-family: 'Geist', sans-serif` on `body`
- Common sizes: 42px (hero title), 32px (page title), 28px (section title), 24px (section header), 22px (admin page title), 20px (subsection), 18px (hero subtitle), 16px (body large), 14px (body), 13px (small/breadcrumb), 12px (footer)

### Icon Library

- **Material Symbols Rounded** — used for all icons
  ```html
  <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded" rel="stylesheet">
  ```
- Usage: `<span class="material-symbols-rounded">search</span>`
- Common icons: `search`, `movie`, `close`, `trophy`, `arrow_forward`, `chevron_right`

### MediaCard Design

From homepage card grid (8 cards, gap 16px):
- Layout: `vertical`, `gap: 10`, `width: fill_container`
- Poster: `aspect-[2/3]`, `w-full`, `object-cover`, `rounded-lg`
- Score badge: positioned over poster bottom-right
  - Green `#22c55e` for ≥ 8.0, Yellow `#eab308` for ≥ 6.0, Red `#ef4444` for < 6.0, Grey `#6B6B70` for null
- Title: `fontSize: 13`, `fontWeight: 500`, `color: #FFFFFF`, single line truncate

### Pagination Design

From movie list page:
- Button size: `36×36px`, `cornerRadius: 8`
- Active page: `fill: #FF8400`, white text
- Inactive: border `1px solid #2A2A2E`, text `#FFFFFF`
- Prev/Next: same border style, chevron icon
- Gap between buttons: `8px`

## Subtasks & Detailed Guidance

### Subtask T060 – Vite + Vue 3 Project Init

**Purpose**: Initialize the frontend project with all required dependencies.

**Steps**:
1. Create `/frontend` with `npm create vite@latest . -- --template vue-ts`.
2. Install dependencies:
   ```bash
   npm install vue-router@4 pinia axios
   npm install -D tailwindcss postcss autoprefixer @types/node
   npx tailwindcss init -p
   ```
3. Configure `tailwind.config.js`: content paths `["./index.html", "./src/**/*.{vue,ts}"]`.
4. `src/main.ts`: register router + pinia.
5. `vite.config.ts`: set `server.proxy` for `/api` → `VITE_API_BASE_URL` in dev.
6. `.env.example`: `VITE_API_BASE_URL=http://localhost:5000`, `VITE_COS_BASE_URL=https://bucket.cos.region.myqcloud.com`.

**Files**:
- `frontend/package.json`
- `frontend/vite.config.ts`
- `frontend/tailwind.config.js`
- `frontend/src/main.ts`
- `frontend/.env.example`

**Validation**:
- [ ] `npm run dev` starts dev server without errors
- [ ] Tailwind utility classes apply correctly in a test component

---

### Subtask T061 – Axios API Client + Type Wrappers

**Purpose**: Centralized API client with typed response envelopes.

**Steps**:
1. `src/api/client.ts`:
   ```typescript
   import axios from 'axios'
   const client = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL })
   client.interceptors.response.use(
     res => res.data,
     err => Promise.reject(err.response?.data ?? err)
   )
   export default client
   ```
2. `src/api/types.ts`: shared response types:
   ```typescript
   export interface PagedResponse<T> {
     data: T[]
     pagination: { page: number; pageSize: number; total: number; totalPages: number }
   }
   export interface ApiError { message: string; errors?: Record<string, string[]> }
   ```
3. `src/utils/cos.ts`:
   ```typescript
   export const cosUrl = (key: string | null | undefined): string =>
     key ? `${import.meta.env.VITE_COS_BASE_URL}/${key}` : '/placeholder.jpg'
   ```
4. `src/api/movies.ts`, `src/api/tv.ts`, `src/api/anime.ts` — stub files with typed fetch functions (filled in per-page WPs).

**Files**:
- `frontend/src/api/client.ts`
- `frontend/src/api/types.ts`
- `frontend/src/utils/cos.ts`

**Validation**:
- [ ] `cosUrl('posters/abc.jpg')` returns full COS URL
- [ ] `cosUrl(null)` returns `/placeholder.jpg`
- [ ] API client response interceptor unwraps `.data`

---

### Subtask T062 – MediaCard Component

**Purpose**: Reusable card component for movies, TV series, and anime list pages.

**Steps**:
1. `src/components/MediaCard.vue`:
   - Props: `id: number`, `contentType: string`, `titleCn: string`, `year?: number`, `posterCosKey?: string`, `doubanScore?: number`, `badge?: string` (e.g., air_status label)
   - Renders: poster image (via `LazyImage`), title, year, score badge
   - Clicking navigates to `/{contentType}/{id}` (use `router-link`)
   - Poster aspect ratio: 2:3 (w-full, aspect-[2/3])
2. Score badge color: green if ≥8.0, yellow if ≥6.0, red if <6.0, grey if null.
3. Optional `badge` slot for content-type-specific labels (air_status, origin_label).

**Files**:
- `frontend/src/components/MediaCard.vue`

**Validation**:
- [ ] Card renders poster, title, year, score
- [ ] Score ≥8.0 shows green badge; null score shows grey "暂无评分"
- [ ] Click navigates to correct route

---

### Subtask T063 – ScoreBar + LazyImage Components

**Purpose**: Score visualization and lazy-loaded image components.

**Steps**:
1. `src/components/ScoreBar.vue`:
   - Props: `score?: number`, `count?: number`
   - Renders large score number + "分" label + rating count
   - Color: `text-green-500` (≥8), `text-yellow-500` (≥6), `text-red-500` (<6), `text-gray-400` (null)
2. `src/components/LazyImage.vue`:
   - Props: `src: string`, `alt: string`, `class?: string`
   - Uses `IntersectionObserver` to load image only when in viewport
   - Shows grey `bg-gray-200 animate-pulse` placeholder until loaded
   - On load error: show placeholder image

**Files**:
- `frontend/src/components/ScoreBar.vue`
- `frontend/src/components/LazyImage.vue`

**Validation**:
- [ ] `LazyImage` shows placeholder before entering viewport
- [ ] `ScoreBar` with score=8.5 shows green; score=5.9 shows red

---

### Subtask T064 – Pagination Component

**Purpose**: Reusable pagination component for all list pages.

**Steps**:
1. `src/components/Pagination.vue`:
   - Props: `page: number`, `totalPages: number`, `total: number`
   - Emits: `update:page` (v-model compatible)
   - Renders: prev button, page numbers (show ±2 around current + first/last with ellipsis), next button
   - Disables prev on page 1, next on last page
2. Page number display logic:
   - Always show pages 1 and `totalPages`
   - Show `...` when gap > 1
   - Show current ±2 pages

**Files**:
- `frontend/src/components/Pagination.vue`

**Validation**:
- [ ] Page 1 of 10: shows [1] 2 3 ... 10; prev disabled
- [ ] Page 5 of 10: shows 1 ... 3 4 [5] 6 7 ... 10
- [ ] Emits `update:page` on click

---

### Subtask T065 – Vue Router Setup + Route Definitions

**Purpose**: Define all application routes with lazy-loaded page components.

**Steps**:
1. `src/router/index.ts` with all routes:
   ```typescript
   const routes = [
     { path: '/', component: () => import('../pages/HomePage.vue') },
     { path: '/movies', component: () => import('../pages/MovieListPage.vue') },
     { path: '/movies/:id', component: () => import('../pages/MovieDetailPage.vue') },
     { path: '/tv', component: () => import('../pages/TvListPage.vue') },
     { path: '/tv/:id', component: () => import('../pages/TvDetailPage.vue') },
     { path: '/tv/:id/seasons/:n', component: () => import('../pages/SeasonDetailPage.vue') },
     { path: '/anime', component: () => import('../pages/AnimeListPage.vue') },
     { path: '/anime/:id', component: () => import('../pages/AnimeDetailPage.vue') },
     { path: '/anime/:id/seasons/:n', component: () => import('../pages/AnimeSeasonDetailPage.vue') },
     { path: '/people/:id', component: () => import('../pages/PersonDetailPage.vue') },
     { path: '/franchises/:id', component: () => import('../pages/FranchiseDetailPage.vue') },
     { path: '/awards/:slug', component: () => import('../pages/AwardsPage.vue') },
     { path: '/awards/:slug/:edition', component: () => import('../pages/CeremonyDetailPage.vue') },
     { path: '/search', component: () => import('../pages/SearchPage.vue') },
     { path: '/rankings', component: () => import('../pages/RankingsPage.vue') },
     { path: '/:pathMatch(.*)*', component: () => import('../pages/NotFoundPage.vue') },
   ]
   ```
2. Create stub page components (empty `<template><div>TODO</div></template>`) for all routes.
3. Scroll behavior: `scrollBehavior: () => ({ top: 0 })`.

**Files**:
- `frontend/src/router/index.ts`
- `frontend/src/pages/*.vue` (stub files for all routes)

**Validation**:
- [ ] Navigating to `/movies` renders MovieListPage stub
- [ ] Unknown route renders NotFoundPage
- [ ] All routes lazy-loaded (code splitting in build output)

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| COS URL construction varies by region | Centralize in `cosUrl()` util; single place to update |
| IntersectionObserver not supported in old browsers | Add polyfill or fallback to eager loading |
| Too many stub page files to maintain | Stubs are minimal (5 lines each); filled in by subsequent WPs |

## Review Guidance

- `MediaCard` is the most-used component — get it right before building list pages
- `cosUrl(null)` must return placeholder (never throw)
- All page components lazy-loaded via dynamic import (verify in build output)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
