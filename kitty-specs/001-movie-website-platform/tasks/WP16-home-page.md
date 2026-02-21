---
work_package_id: WP16
title: Home Page
lane: planned
dependencies:
- WP14
subtasks:
- T071
- T072
- T073
- T074
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

# Work Package Prompt: WP16 – Home Page

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP16 --base WP15
```

---

## Objectives & Success Criteria

- Hero banner carousel auto-plays featured banners (5s interval); manual prev/next controls
- "最新上映" section shows 10 recent movies as horizontal scroll row of MediaCards
- "热门剧集" and "热门动漫" sections each show 6 cards
- "本周热榜" section shows top-10 hot movies with rank numbers (gold/silver/bronze for 1-3)
- Page calls `GET /api/v1/home` and renders all sections from single response

## Context & Constraints

- **Spec**: Scenario 1 (homepage), FR-1 (banner), FR-2 (latest movies), FR-3 (hot rankings preview)
- Home API response: `{banners, latestMovies, hotTv, hotAnime, hotMoviesTop10}`
- Banner auto-play: pause on hover; resume on mouse leave
- Horizontal scroll rows: `overflow-x-auto` with `flex gap-4`; hide scrollbar on desktop
- Rank badges: 1=gold(`#FFD700`), 2=silver(`#C0C0C0`), 3=bronze(`#CD7F32`), 4+=grey number

## Design Reference

**Source**: `design/design.pen` — frame `UmLXy` (Homepage - 首页), width 1440px

### Hero Banner (frame `Fmlca`)

```
height: 560px
layout: none (absolute positioning)
clip: true
width: fill_container
```

Layers (bottom to top):
1. **Backdrop image** (`snoDi`): `1440×560px`, `object-cover`
2. **Bottom gradient** (`Hnyj4`): `linear-gradient(0deg, #0B0B0EFF 60%, #0B0B0E00 100%)`
3. **Left gradient** (`SX4V9`): `linear-gradient(90deg, #0B0B0EDD 0%, #0B0B0E00 60%)`
4. **Hero content** (`xd3yU`): positioned at `x:60, y:200`, `width: 560px`, `layout: vertical`, `gap: 16px`
   - Tag chip: genre badge, `gap: 8px`
   - Title: `fontSize: 42, fontWeight: 700, color: #FFFFFF, lineHeight: 1.2`
   - Subtitle (original title): `fontSize: 18, color: #B8B9B6`
   - Meta row (year/rating/duration): `gap: 16px`, `alignItems: center`
   - Description: `fontSize: 14, color: #8E8E93, lineHeight: 1.6, width: 480px`
5. **Dot indicators** (`S1iKX`): positioned at `x:60, y:520`, `gap: 8px`
   - Active dot: `#FF8400`, `24×4px`, `borderRadius: 2px`
   - Inactive dot: `#4A4A50`, `8×4px`, `borderRadius: 2px`

### Content Sections Layout

Page is `layout: vertical`, `fill: #0B0B0E`

| Section | Frame | Padding | Gap |
|---------|-------|---------|-----|
| 近期热门电影 | `5mGvx` | `[40, 60]` | 24 |
| 近期热门电视剧 | `ozzuA` | `[20, 60, 40, 60]` | 24 |
| 近期热门动漫 | `KGjPu` | `[20, 60, 40, 60]` | 24 |
| 高分榜 & 奖项入口 | `56Yx9` | `[20, 60, 40, 60]` | 24 |

### Section Header Pattern

```
justify-content: space-between
width: fill_container
```
- Title: `fontSize: 24, fontWeight: 700, color: #FFFFFF`
- "查看更多" link: `gap: 4px`, `alignItems: center`, text `#B8B9B6` + arrow icon `#B8B9B6`

### Card Grid (8 columns)

```
display: grid
grid-template-columns: repeat(8, 1fr)
gap: 16px
width: fill_container
```

Each card: `layout: vertical`, `gap: 10`, `width: fill_container`

### Rankings Entry Card (frame `s6ki1`)

```
cornerRadius: 12
fill: #16161A
border: 1px solid #2A2A2E
padding: 32
height: 200px
layout: vertical
justify-content: center
gap: 12
```

- Trophy icon: `#FF8400`, 40×40px (Material Symbols Rounded `trophy`)
- Title: `fontSize: 24, fontWeight: 700, color: #FFFFFF`
- Description: `fontSize: 14, color: #8E8E93, lineHeight: 1.5`
- CTA link: `gap: 4px`, `color: #FF8400`

### Awards Entry Cards (frame `7fx4Y`, 3 cards, gap 16)

Each award card: `cornerRadius: 12`, `height: 200px`, `padding: 24`, `border: 1px solid #2A2A2E`, `layout: vertical`, `justify-content: end`, `gap: 8`

Gradient fills:
- 奥斯卡: `linear-gradient(0deg, #1A1A1EFF 70%, #FF840033 100%)`
- 戛纳: `linear-gradient(0deg, #1A1A1EFF 70%, #6366F133 100%)`
- 金马奖: `linear-gradient(0deg, #1A1A1EFF 70%, #32D58333 100%)`

## Subtasks & Detailed Guidance

### Subtask T071 – Home API Integration

**Purpose**: Fetch home page data and expose to child components.

**Steps**:
1. `src/api/home.ts`:
   ```typescript
   export interface HomeResponse {
     banners: Banner[]
     latestMovies: MediaCard[]
     hotTv: MediaCard[]
     hotAnime: MediaCard[]
     hotMoviesTop10: RankedItem[]
   }
   export const getHome = () => client.get<HomeResponse>('/api/v1/home')
   ```
2. `src/pages/HomePage.vue`:
   - `onMounted`: call `getHome()`; store in `homeData` ref
   - Show skeleton loaders while loading
   - `usePageMeta('首页', '发现最新最热的电影、剧集和动漫')`

**Files**:
- `frontend/src/api/home.ts`
- `frontend/src/pages/HomePage.vue`

**Validation**:
- [ ] Page loads without errors; all sections populated from API
- [ ] Loading state shows skeleton placeholders

---

### Subtask T072 – Hero Banner Carousel

**Purpose**: Auto-playing banner carousel with manual controls.

**Steps**:
1. `src/components/BannerCarousel.vue`:
   - Props: `banners: Banner[]`
   - Auto-advance every 5 seconds using `setInterval`; clear on `onUnmounted`
   - Pause on `@mouseenter`; resume on `@mouseleave`
   - Prev/next arrow buttons (absolute positioned left/right)
   - Dot indicators at bottom (click to jump to slide)
   - Banner image: full-width, aspect-[16/6], `object-cover`
   - Overlay: gradient from transparent to `rgba(0,0,0,0.7)` at bottom; title + subtitle text
   - `link_url`: wrap in `<a>` tag (external link, `target="_blank"`)
2. Transition: CSS `transition-transform` slide effect.

**Files**:
- `frontend/src/components/BannerCarousel.vue`

**Validation**:
- [ ] Auto-advances every 5 seconds
- [ ] Pauses on hover; resumes on leave
- [ ] Dot indicators reflect current slide
- [ ] Manual prev/next works

---

### Subtask T073 – Content Row Sections

**Purpose**: Horizontal scrollable rows for latest movies, hot TV, hot anime.

**Steps**:
1. `src/components/ContentRow.vue`:
   - Props: `title: string`, `items: MediaCard[]`, `viewAllLink: string`
   - Renders section heading with "查看全部 →" link
   - Horizontal scroll: `flex overflow-x-auto gap-3 pb-2 scrollbar-hide`
   - Each item: `<MediaCard>` with fixed width `w-36` (144px)
2. Add `scrollbar-hide` utility to Tailwind config (plugin or custom CSS):
   ```css
   .scrollbar-hide::-webkit-scrollbar { display: none; }
   .scrollbar-hide { -ms-overflow-style: none; scrollbar-width: none; }
   ```
3. Use `ContentRow` in `HomePage.vue` for latestMovies, hotTv, hotAnime sections.

**Files**:
- `frontend/src/components/ContentRow.vue`

**Validation**:
- [ ] Row scrolls horizontally on overflow
- [ ] Scrollbar hidden on desktop
- [ ] "查看全部" link navigates to correct list page

---

### Subtask T074 – Hot Rankings Preview Section

**Purpose**: Top-10 hot movies list with rank badges.

**Steps**:
1. `src/components/RankingPreview.vue`:
   - Props: `items: RankedItem[]` (top 10)
   - Renders vertical list with rank number + poster thumbnail (40×60px) + title + score
   - Rank badge colors: 1=`text-yellow-400`, 2=`text-gray-300`, 3=`text-amber-600`, 4+=`text-gray-500`
   - "查看完整榜单 →" link to `/rankings`
2. Use in `HomePage.vue` with `hotMoviesTop10` data.

**Files**:
- `frontend/src/components/RankingPreview.vue`

**Validation**:
- [ ] Rank 1 shows gold color; rank 4+ shows grey
- [ ] Each item links to `/movies/{id}`
- [ ] "查看完整榜单" links to `/rankings`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Banner carousel memory leak (setInterval not cleared) | Clear interval in `onUnmounted` |
| Home API slow (multiple DB queries) | API caches home response; frontend shows skeletons |
| Horizontal scroll janky on mobile | Use `scroll-snap-type: x mandatory` on container |

## Review Guidance

- Banner: pause on hover is required (spec FR-1)
- Rank colors: 1=gold, 2=silver, 3=bronze (not just "top 3 are colored")
- ContentRow "查看全部" links: movies→`/movies`, tv→`/tv`, anime→`/anime`

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
