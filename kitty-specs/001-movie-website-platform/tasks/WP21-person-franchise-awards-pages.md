---
work_package_id: WP21
title: Person, Franchise + Awards Pages
lane: planned
dependencies:
- WP14
subtasks:
- T091
- T092
- T093
- T094
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

# Work Package Prompt: WP21 – Person, Franchise + Awards Pages

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP21 --base WP15
```

---

## Objectives & Success Criteria

- `/people/:id` shows person profile: biography (foldable >200 chars), works tabs (全部/导演/编剧/演员), top-8 collaborators, photo wall
- `/franchises/:id` shows franchise overview with movies in order
- `/awards/:slug` shows award event with ceremony list
- `/awards/:slug/:edition` shows ceremony nominations grouped by category; winners highlighted

## Context & Constraints

- **Spec**: Scenarios 7 (awards), 13 (person detail), FR-3 (person sections), FR-14 (awards slugs), FR-16 (collaborators), FR-17 (works tabs)
- Person biography: fold at 200 chars with "展开简介" toggle
- Works tabs filter by role; tab counts shown in badge
- Collaborator cards: avatar + name + co-work count
- Awards: winner nominations have gold star/trophy icon; non-winners are grey

## Subtasks & Detailed Guidance

### Subtask T091 – Person Detail Page

**Purpose**: Full person profile with works tabs, collaborators, and photo wall.

**Steps**:
1. `src/pages/PersonDetailPage.vue`:
   - Fetch `GET /api/v1/people/:id` (with optional `?role=` param)
   - Header: avatar (120×120px circle) + name (CN + EN) + birth date + nationality + professions tags
   - Biography: `line-clamp-4` with "展开简介" toggle (same pattern as movie synopsis)
2. Works tabs:
   ```vue
   <div class="flex gap-2 border-b border-gray-700 mb-4">
     <button v-for="tab in worksTabs" :key="tab.role"
       :class="activeRole === tab.role ? 'border-b-2 border-red-500' : ''"
       @click="setRole(tab.role)">
       {{ tab.label }} ({{ tab.count }})
     </button>
   </div>
   ```
   Tabs: 全部/导演/编剧/演员; clicking updates `?role=` query param and re-fetches.
3. Works grid: `grid grid-cols-3 md:grid-cols-5`; each card: poster + title + year + role.
4. Top-8 collaborators: horizontal scroll row; each card: avatar + name + "合作{n}次".
5. Photo wall: `grid grid-cols-4 md:grid-cols-6`; click opens `ImageLightbox`.
6. `usePageMeta(computed(() => person.value?.nameCn ?? '加载中'))`.

**Files**:
- `frontend/src/pages/PersonDetailPage.vue`

**Validation**:
- [ ] Works tab "演员" filters to actor credits only
- [ ] Tab counts reflect number of works per role
- [ ] Photo wall opens lightbox on click
- [ ] Collaborator shows co-work count

---

### Subtask T092 – Franchise Detail Page

**Purpose**: Franchise overview with ordered movie list.

**Steps**:
1. `src/pages/FranchiseDetailPage.vue`:
   - Fetch `GET /api/v1/franchises/:id`
   - Header: franchise name + description
   - Movie list: ordered by `franchise_order ASC`; each item: poster + title + year + douban_score
   - Layout: vertical list (not grid) to show order clearly; rank number shown left of poster
2. `src/api/franchises.ts`: `getFranchise(id)`.

**Files**:
- `frontend/src/pages/FranchiseDetailPage.vue`
- `frontend/src/api/franchises.ts`

**Validation**:
- [ ] Movies shown in franchise_order sequence
- [ ] Each movie links to `/movies/{id}`
- [ ] Franchise name shown as page heading

---

### Subtask T093 – Awards Main Page

**Purpose**: Award event overview with ceremony list.

**Steps**:
1. `src/pages/AwardsPage.vue`:
   - Route: `/awards/:slug`
   - Fetch `GET /api/v1/awards/:slug`; 404 → NotFoundPage
   - Header: award name (CN + EN) + description + official URL link
   - Ceremony list: table with columns 届次, 年份, 日期; each row links to `/awards/:slug/:edition`
   - Ceremonies ordered newest first (API returns `edition_number DESC`)
2. `src/api/awards.ts`: `getAwardEvent(slug)`, `getCeremonyDetail(slug, edition)`.

**Files**:
- `frontend/src/pages/AwardsPage.vue`
- `frontend/src/api/awards.ts`

**Validation**:
- [ ] `/awards/oscar` shows Oscar event info + ceremony list
- [ ] `/awards/nonexistent` shows 404 page
- [ ] Ceremony rows link to correct edition URLs

---

### Subtask T094 – Ceremony Detail Page

**Purpose**: Ceremony nominations grouped by category with winner highlighting.

**Steps**:
1. `src/pages/CeremonyDetailPage.vue`:
   - Route: `/awards/:slug/:edition`
   - Fetch `GET /api/v1/awards/:slug/:edition`
   - Header: event name + edition number + ceremony date
   - Prev/next edition navigation: `← 第{prevEdition}届` / `第{nextEdition}届 →`
   - Categories: each as a collapsible section (expanded by default)
   - Within category: nominations list; winner row has gold trophy icon + bold text; non-winner is grey
   - Each nomination: content poster thumbnail + title + person name (if applicable) + note
2. Winner styling:
   ```vue
   <div :class="nom.isWinner ? 'border-l-4 border-yellow-400 pl-3' : 'pl-3 opacity-60'">
   ```

**Files**:
- `frontend/src/pages/CeremonyDetailPage.vue`

**Validation**:
- [ ] Winners shown first in each category with gold accent
- [ ] Non-winners shown with reduced opacity
- [ ] Prev/next edition navigation works
- [ ] `usePageMeta` sets title to `第{n}届{awardName} - 影视网`

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Person works list very long (500+ credits) | API limits to 100; show "仅显示前100条" notice |
| Awards ceremony with 50+ categories | All expanded by default may be overwhelming; add "全部折叠/展开" toggle |
| Franchise page with no description | Hide description section with `v-if` |

## Review Guidance

- Person works tabs: re-fetch API with `?role=` param on tab change (not client-side filter)
- Awards winners: gold left border + full opacity; non-winners: reduced opacity (not hidden)
- Ceremony prev/next: use actual edition numbers from API (not arithmetic ±1)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
