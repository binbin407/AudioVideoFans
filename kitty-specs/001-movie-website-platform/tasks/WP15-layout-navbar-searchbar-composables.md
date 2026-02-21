---
work_package_id: "WP15"
subtasks:
  - "T066"
  - "T067"
  - "T068"
  - "T069"
  - "T070"
title: "Layout, Navbar, Searchbar + Composables"
phase: "Phase 4 - Frontend"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP14"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Prompt generated via /spec-kitty.tasks"
---

# Work Package Prompt: WP15 – Layout, Navbar, Searchbar + Composables

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP15 --base WP14
```

---

## Objectives & Success Criteria

- `AppLayout.vue` provides sticky navbar + main content area + footer used by all pages
- Navbar has logo, nav links (首页/电影/剧集/动漫/排行榜), and search icon that expands search bar
- Search bar shows autocomplete dropdown with grouped results (≤3 per type) within 100ms
- `useFilters` composable manages URL query params ↔ reactive filter state for list pages
- `usePageMeta` composable sets `<title>` and meta description for SEO

## Context & Constraints

- **Spec**: Scenario 1 (homepage), FR-18 (search autocomplete), FR-43 (search)
- Navbar is sticky (`position: sticky; top: 0`); dark background (`bg-gray-900`)
- Autocomplete: debounce 300ms; min 1 character; call `GET /api/v1/search/autocomplete?q=`
- Filter state synced to URL query params (so filters survive page refresh / sharing)
- `usePageMeta` uses `@vueuse/head` or direct `document.title` assignment

## Design Reference

**Source**: `design/design.pen` — Navigation frame present on every public page

### Navbar (frame `F7Irx` / `pM3hn` / `nZMFc` etc.)

```
height: 64px
padding: [0, 60]          ← 60px horizontal padding
background: #0B0B0ECC     ← semi-transparent dark (backdrop-blur recommended)
border-bottom: 1px solid #2A2A2E
position: sticky, top: 0, z-index: 50
justify-content: space-between
```

- **Left side** (`navLeft`): logo group + nav links, `gap: 32px`
  - Logo group: `#FF8400` movie icon (Material Symbols Rounded `movie`, 20px) + text "影视网" `fontSize: 14, fontWeight: 600, color: #FF8400`, `gap: 8px`
  - Nav links: 首页 / 电影 / 剧集 / 动漫 / 排行榜, `gap: 24px`, `fontSize: 14`, `color: #B8B9B6`; active link `color: #FFFFFF`
- **Right side** (`navRight`): search icon `Material Symbols Rounded "search"`, `color: #B8B9B6`, `24×24px`, `gap: 16px`

### SearchBar (frame `ivRXO` in Search Results page)

```
height: 52px
background: #16161A
cornerRadius: 12
padding: [0, 20]
border: 2px solid #FF8400   ← active/focused state
gap: 12px
```

- Search icon: `#FF8400`, 22px
- Input text: `fontSize: 16`, `color: #FFFFFF`
- Clear icon: `#6B6B70`, 20px (Material Symbols Rounded `close`)

### Autocomplete Dropdown (frame `YLk79`)

```
background: #16161A
cornerRadius: 12
border: 1px solid #2A2A2E
layout: vertical
```

- Section title row: `padding: [10, 16]`, `color: #6B6B70`, `fontSize: 13`
- Item row: `padding: [10, 16]`, `gap: 12px`; highlighted item `fill: #1E1E22`
- Item: thumbnail (24×36px) + title + year

### Footer (frame `7Glqi` / `oPG4g`)

```
height: 80px
padding: [0, 60]
border-top: 1px solid #2A2A2E
background: #0B0B0E (public) / #0A0A0D (some pages)
justify-content: space-between
```

- Left: logo icon + "影视网" `fontSize: 14, fontWeight: 600, color: #6B6B70` + copyright `fontSize: 12, color: #4A4A50`, `gap: 8px`
- Right: 关于我们 / 联系方式 / 隐私政策, `fontSize: 12, color: #6B6B70`, `gap: 24px`

### Breadcrumb (present on list/detail pages)

```
fontSize: 13
gap: 8px
```

- Inactive crumb: `color: #6B6B70`
- Separator `/`: `color: #4A4A50`
- Current page: `color: #FFFFFF`

## Subtasks & Detailed Guidance

### Subtask T066 – AppLayout + Footer

**Purpose**: Shell layout component wrapping all pages.

**Steps**:
1. `src/layouts/AppLayout.vue`:
   - Sticky navbar at top (`z-50`)
   - `<slot />` for page content
   - Footer with site name, copyright, links (关于/联系)
2. `src/App.vue`: use `<AppLayout>` wrapping `<RouterView>`.
3. Global CSS in `src/assets/main.css`: import Tailwind directives; set `body { background: #111; color: #eee; }` (dark theme).

**Files**:
- `frontend/src/layouts/AppLayout.vue`
- `frontend/src/App.vue`
- `frontend/src/assets/main.css`

**Validation**:
- [ ] All pages show navbar and footer
- [ ] Body has dark background

---

### Subtask T067 – Navbar Component

**Purpose**: Top navigation bar with logo, links, and search toggle.

**Steps**:
1. `src/components/Navbar.vue`:
   - Logo: text "影视网" linking to `/`
   - Nav links: 首页(`/`), 电影(`/movies`), 剧集(`/tv`), 动漫(`/anime`), 排行榜(`/rankings`)
   - Active link highlighted (use `router-link-active` class)
   - Search icon (magnifying glass SVG) on right; clicking toggles `SearchBar` visibility
2. Mobile: hamburger menu collapses nav links (Tailwind `md:flex hidden`).
3. Navbar background: `bg-gray-900 border-b border-gray-700`.

**Files**:
- `frontend/src/components/Navbar.vue`

**Validation**:
- [ ] Active route link is visually highlighted
- [ ] Search icon click shows/hides search bar
- [ ] Mobile: nav links hidden below `md` breakpoint

---

### Subtask T068 – SearchBar + Autocomplete Dropdown

**Purpose**: Search input with live autocomplete grouped by content type.

**Steps**:
1. `src/components/SearchBar.vue`:
   - Input with `v-model` bound to `query` ref
   - Debounce 300ms using `setTimeout`/`clearTimeout`
   - On input (≥1 char): call `GET /api/v1/search/autocomplete?q={query}`
   - Show dropdown with sections: 电影, 剧集, 动漫, 人物 (skip empty sections)
   - Each item: poster thumbnail (24×36px) + title + year
   - "查看全部结果" link at bottom → `/search?q={query}`
   - Press Enter or click "查看全部结果" → navigate to `/search?q={query}`
   - Close dropdown on Escape or click outside (use `@click.outside` or `onMounted` listener)
2. `src/api/search.ts`:
   ```typescript
   export const autocomplete = (q: string) =>
     client.get<AutocompleteResponse>('/api/v1/search/autocomplete', { params: { q } })
   ```

**Files**:
- `frontend/src/components/SearchBar.vue`
- `frontend/src/api/search.ts`

**Validation**:
- [ ] Typing "星" shows grouped autocomplete results
- [ ] Empty sections not shown in dropdown
- [ ] Enter key navigates to search page
- [ ] Dropdown closes on Escape

---

### Subtask T069 – useFilters Composable

**Purpose**: Sync list page filter state with URL query params.

**Steps**:
1. `src/composables/useFilters.ts`:
   ```typescript
   export function useFilters<T extends Record<string, any>>(defaults: T) {
     const route = useRoute()
     const router = useRouter()
     const filters = reactive({ ...defaults })

     // Initialize from URL on mount
     onMounted(() => {
       Object.keys(defaults).forEach(key => {
         if (route.query[key]) filters[key] = route.query[key]
       })
     })

     // Sync to URL on filter change
     watch(filters, (val) => {
       router.replace({ query: { ...val, page: undefined } })
     }, { deep: true })

     const reset = () => Object.assign(filters, defaults)
     return { filters, reset }
   }
   ```
2. Usage in list pages: `const { filters } = useFilters({ genre: '', region: '', sort: 'douban_score' })`.
3. Array filters (genres multi-select): handle `string | string[]` from `route.query`.

**Files**:
- `frontend/src/composables/useFilters.ts`

**Validation**:
- [ ] Selecting genre updates URL query param
- [ ] Refreshing page restores filter state from URL
- [ ] Changing filter resets to page 1

---

### Subtask T070 – usePageMeta Composable

**Purpose**: Set page `<title>` and meta description for SEO.

**Steps**:
1. `src/composables/usePageMeta.ts`:
   ```typescript
   export function usePageMeta(title: MaybeRef<string>, description?: MaybeRef<string>) {
     watchEffect(() => {
       document.title = `${unref(title)} - 影视网`
       const meta = document.querySelector('meta[name="description"]')
       if (meta && description) meta.setAttribute('content', unref(description) ?? '')
     })
   }
   ```
2. Usage: `usePageMeta(computed(() => movie.value?.titleCn ?? '加载中'))`.
3. Ensure `index.html` has `<meta name="description" content="">` placeholder.

**Files**:
- `frontend/src/composables/usePageMeta.ts`
- `frontend/index.html` (add meta description tag)

**Validation**:
- [ ] Movie detail page sets title to `{titleCn} - 影视网`
- [ ] Title updates reactively when data loads
- [ ] Meta description tag updated

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Autocomplete API called on every keystroke | 300ms debounce; cancel previous request on new input |
| Filter URL sync causes infinite router loop | Use `router.replace` (not push); guard with `if (JSON.stringify(current) !== JSON.stringify(new))` |
| `document.title` SSR incompatible | Acceptable for CSR-only app; note for future SSR migration |

## Review Guidance

- Autocomplete dropdown: empty sections hidden (not shown as empty headers)
- `useFilters`: array params (genres) must handle both `string` and `string[]` from URL
- `usePageMeta`: reactive — title updates when async data resolves

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
