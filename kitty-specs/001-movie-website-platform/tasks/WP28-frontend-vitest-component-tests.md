---
work_package_id: "WP28"
subtasks:
  - "T121"
  - "T122"
  - "T123"
  - "T124"
  - "T125"
title: "Frontend Vitest Component Tests"
phase: "Phase 8 - Testing"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP14", "WP15", "WP16", "WP18"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Created via /spec-kitty.analyze remediation (C3: constitution test coverage)"
---

# Work Package Prompt: WP28 – Frontend Vitest Component Tests

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP28 --base WP18
```

---

## Objectives & Success Criteria

- Vitest configured in `frontend/vite.config.ts` with `@vue/test-utils` and `jsdom`
- All 5 key component groups tested: MediaCard, FilterBar, Pagination, BannerCarousel, SearchBar
- `npm run test` in `/frontend` passes with zero failures
- Tests run in CI headlessly (`vitest run --reporter=verbose`)
- Coverage report generated (`v8` provider): `npm run test:coverage`

## Context & Constraints

- **Constitution**: Vitest for key components — cards, filters, pagination (mandatory); named components from plan.md `frontend/tests/components/`
- No full E2E (Playwright/Cypress) in scope for this WP — unit/component tests only
- Use `@vue/test-utils` `mount()` / `shallowMount()` as appropriate; prefer `mount()` for interaction tests
- Mock all API calls with `vi.mock()` — no real HTTP requests in component tests
- Mock `vue-router` using `@vue/test-utils` `global.plugins` with a stub router
- Target: one test file per component, covering render, props, emits, and key interactions

---

## Subtasks & Detailed Guidance

### Subtask T121 – Vitest + Vue Test Utils Setup

**Purpose**: Configure Vitest in the frontend project with all required plugins and global setup.

**Steps**:
1. Install dev dependencies in `frontend/`:
   ```bash
   npm install -D vitest @vue/test-utils @vitest/coverage-v8 jsdom @testing-library/vue happy-dom
   ```
2. Update `frontend/vite.config.ts`:
   ```typescript
   import { defineConfig } from 'vite'
   import vue from '@vitejs/plugin-vue'

   export default defineConfig({
     plugins: [vue()],
     test: {
       environment: 'jsdom',
       globals: true,
       setupFiles: ['./tests/setup.ts'],
       coverage: {
         provider: 'v8',
         reporter: ['text', 'html'],
         include: ['src/components/**', 'src/composables/**'],
       },
     },
   })
   ```
3. `frontend/tests/setup.ts`:
   ```typescript
   import { config } from '@vue/test-utils'
   import { createRouter, createMemoryHistory } from 'vue-router'

   // Global stub for router-link and router-view
   const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: {} }] })
   config.global.plugins = [router]

   // Mock window.matchMedia (jsdom doesn't implement it)
   Object.defineProperty(window, 'matchMedia', {
     value: vi.fn().mockReturnValue({ matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() }),
   })
   ```
4. Add scripts to `frontend/package.json`:
   ```json
   "test": "vitest run",
   "test:watch": "vitest",
   "test:coverage": "vitest run --coverage"
   ```
5. Create `frontend/tests/components/` directory (mirror of `src/components/`).

**Files**:
- `frontend/vite.config.ts` (update `test` section)
- `frontend/tests/setup.ts`
- `frontend/package.json` (update scripts)

**Validation**:
- [ ] `npm run test` in `frontend/` runs and exits 0 with no test files found (yet)
- [ ] `npm run test:coverage` generates `frontend/coverage/index.html`
- [ ] jsdom environment resolves `document`, `window` in tests

---

### Subtask T122 – MediaCard + ImageTabBlock Tests

**Purpose**: Test the primary content display components (most frequently used across the site).

**Steps**:
1. `frontend/tests/components/MediaCard.test.ts`:
   - **Renders with full props**:
     ```typescript
     it('renders title, year, and score when all props provided', () => {
       const wrapper = mount(MediaCard, { props: { id: 1, contentType: 'movie',
         titleCn: '复仇者联盟', year: 2019, doubanScore: 8.5, posterCosKey: 'posters/abc.jpg' } })
       expect(wrapper.find('[data-testid="title"]').text()).toBe('复仇者联盟')
       expect(wrapper.find('[data-testid="score"]').text()).toContain('8.5')
     })
     ```
   - **Hides score area when `doubanScore` is null** (spec requirement):
     ```typescript
     it('does not render score element when doubanScore is null', () => {
       const wrapper = mount(MediaCard, { props: { ..., doubanScore: null } })
       expect(wrapper.find('[data-testid="score"]').exists()).toBe(false)
     })
     ```
   - **Shows grey placeholder on image error**:
     ```typescript
     it('shows fallback placeholder when image fails to load', async () => {
       const wrapper = mount(MediaCard, { props: { ..., posterCosKey: 'invalid.jpg' } })
       await wrapper.find('img').trigger('error')
       expect(wrapper.find('[data-testid="placeholder"]').exists()).toBe(true)
     })
     ```
   - **Router-link navigates to correct path**: verify `to` prop = `/movies/1`
2. `frontend/tests/components/ImageTabBlock.test.ts`:
   - Renders correct tab count badge
   - Hides tab when `images.length === 0`
   - Default active tab is first non-empty tab
   - Clicking image emits `open-lightbox` with correct index

**Files**:
- `frontend/tests/components/MediaCard.test.ts`
- `frontend/tests/components/ImageTabBlock.test.ts`

**Validation**:
- [ ] `doubanScore: null` → score element absent (critical spec requirement)
- [ ] Image error handler shows placeholder (not broken img tag)
- [ ] All 4 test cases for MediaCard pass

---

### Subtask T123 – FilterBar + Pagination Tests

**Purpose**: Test the two core list-page interaction components (constitution explicitly names these).

**Steps**:
1. `frontend/tests/components/FilterBar.test.ts`:
   - **Renders all filter rows** with correct labels and tag count
   - **Single click selects tag and highlights it**:
     ```typescript
     it('highlights clicked tag with orange class', async () => {
       const wrapper = mount(FilterBar, { props: { rows: [{ label: '类型', tags: ['科幻', '动作'] }] } })
       await wrapper.find('[data-testid="tag-科幻"]').trigger('click')
       expect(wrapper.find('[data-testid="tag-科幻"]').classes()).toContain('bg-orange-500')
     })
     ```
   - **Emits `filter-change` with correct payload** on tag click
   - **Multi-select**: clicking two tags in same row both become active
   - **Single-select row**: clicking second tag deselects first (when `multiSelect: false`)
   - **`clearFilters()`**: all tags deselected, `filter-change` emits empty object
2. `frontend/tests/components/Pagination.test.ts`:
   - **Renders correct page numbers** for total=50, pageSize=24 → 3 pages
   - **Emits `page-change`** with correct page number on button click
   - **Disables prev on page 1** and next on last page
   - **Ellipsis shown** when total pages > 7 (e.g., current=5, total=10 shows `1 … 4 5 6 … 10`)
   - **Active page highlighted**: current page button has active styling

**Files**:
- `frontend/tests/components/FilterBar.test.ts`
- `frontend/tests/components/Pagination.test.ts`

**Validation**:
- [ ] FilterBar multi-select and single-select modes both tested
- [ ] `filter-change` event payload verified (not just that it fires)
- [ ] Pagination ellipsis logic tested for large page counts

---

### Subtask T124 – SearchBar + Autocomplete Tests

**Purpose**: Test the search input with debounced autocomplete and keyboard navigation.

**Steps**:
1. `frontend/tests/components/SearchBar.test.ts`:
   - **Mock API**: `vi.mock('../../src/api/search', () => ({ autocomplete: vi.fn().mockResolvedValue({ movies: [{id:1, title_cn:'星际穿越'}], tv: [], anime: [], people: [] }) }))`
   - **Renders input field and search icon**
   - **Typing triggers autocomplete after debounce**:
     ```typescript
     it('calls autocomplete API after 300ms debounce', async () => {
       vi.useFakeTimers()
       const wrapper = mount(SearchBar)
       await wrapper.find('input').setValue('星际')
       vi.advanceTimersByTime(300)
       await nextTick()
       expect(autocomplete).toHaveBeenCalledWith('星际')
       vi.useRealTimers()
     })
     ```
   - **Dropdown shows grouped results** after API resolves
   - **Clicking result item navigates** to correct detail URL (verify `router.push` called)
   - **Enter key** navigates to `/search?q={query}`
   - **Escape key** or click-outside **closes dropdown**
   - **Empty query**: autocomplete NOT called when input is cleared

**Files**:
- `frontend/tests/components/SearchBar.test.ts`

**Validation**:
- [ ] Debounce tested with `vi.useFakeTimers()`
- [ ] Dropdown hidden when input empty (no API call)
- [ ] Keyboard navigation (Enter → search page) verified
- [ ] Click outside closes dropdown (test by triggering `blur` or clicking document body)

---

### Subtask T125 – BannerCarousel + SynopsisBlock Tests

**Purpose**: Test auto-play timing, pause-on-hover, and synopsis collapse behavior.

**Steps**:
1. `frontend/tests/components/BannerCarousel.test.ts`:
   - **Does not render when `banners` prop is empty array** (spec requirement):
     ```typescript
     it('renders nothing when banners array is empty', () => {
       const wrapper = mount(BannerCarousel, { props: { banners: [] } })
       expect(wrapper.find('[data-testid="banner-section"]').exists()).toBe(false)
     })
     ```
   - **Renders first banner** when array has items
   - **Auto-advances to second slide after 5 seconds**:
     ```typescript
     it('auto-advances slide after 5000ms', async () => {
       vi.useFakeTimers()
       const wrapper = mount(BannerCarousel, { props: { banners: [banner1, banner2] } })
       expect(wrapper.find('[data-testid="slide-0"]').isVisible()).toBe(true)
       vi.advanceTimersByTime(5000)
       await nextTick()
       expect(wrapper.find('[data-testid="slide-1"]').isVisible()).toBe(true)
       vi.useRealTimers()
     })
     ```
   - **Dot indicator click** advances to correct slide
   - **`onUnmounted` clears interval** — verify `clearInterval` called (spy on `window.clearInterval`)
2. `frontend/tests/components/SynopsisBlock.test.ts`:
   - **Short text (<= 150 chars)**: no collapse toggle, full text visible
   - **Long text (> 150 chars)**: text truncated to 150 chars + `…`; 「展开全文」button visible
   - **Click 「展开全文」**: full text shown, button changes to 「收起」
   - **Click 「收起」**: text truncated again

**Files**:
- `frontend/tests/components/BannerCarousel.test.ts`
- `frontend/tests/components/SynopsisBlock.test.ts`

**Validation**:
- [ ] Empty banners → section not rendered (spec requirement tested)
- [ ] 5-second interval timing tested with `vi.useFakeTimers()`
- [ ] `clearInterval` called on unmount (no memory leak)
- [ ] Synopsis collapse threshold at exactly 150 chars (boundary test: 150 = no collapse, 151 = collapse)

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| `jsdom` doesn't support CSS visibility (`isVisible()`) | Use `display: none` toggle instead of CSS opacity; or use `@testing-library/vue` `queryByTestId` |
| Tailwind CSS classes not applied in jsdom | Tests check classes (e.g., `bg-orange-500`) not computed styles — this works in jsdom |
| `vi.useFakeTimers()` not cleaned up between tests | Add `afterEach(() => vi.useRealTimers())` to all timer-dependent test files |
| COS URL helper (`cosUrl()`) returns null in test env | Set `import.meta.env.VITE_COS_CDN_BASE` in `tests/setup.ts` to a test value |

## Review Guidance

- `data-testid` attributes must be added to components where they don't already exist — this is expected and acceptable
- Constitution names 3 component groups explicitly: **cards** (MediaCard), **filters** (FilterBar), **pagination** (Pagination) — these 3 are mandatory; BannerCarousel and SearchBar are additionally covered for good measure
- Do not mock `vue-router` `router.push` at the global level — use `@vue/test-utils` `global.stubs` or a memory router to capture navigation calls
- CI integration: add `npm run test` to `build-and-test.yml` (in WP26's CI stub) as a separate job for the frontend workspace

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Created via analyze remediation (C3).
