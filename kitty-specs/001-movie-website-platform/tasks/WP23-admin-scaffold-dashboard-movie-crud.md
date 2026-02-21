---
work_package_id: WP23
title: Admin Scaffold, Dashboard + Movie CRUD
lane: planned
dependencies: []
subtasks:
- T099
- T100
- T101
- T102
phase: Phase 5 - Admin Frontend
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

# Work Package Prompt: WP23 – Admin Scaffold, Dashboard + Movie CRUD

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP23 --base WP14
```

---

## Objectives & Success Criteria

- `/admin` Vue 3 app with TDesign Vue component library initialized under `/admin`
- JWT login page; token stored in localStorage; axios interceptor adds `Authorization: Bearer` header
- Dashboard shows stats cards (movie/TV/anime/people/pending counts) from `GET /admin/stats`
- Movie list page with keyword search, include_deleted toggle, and edit/delete actions
- Movie create/edit form with all fields + credits management

## Context & Constraints

- **Spec**: FR-22 (admin login), FR-23 (dashboard), FR-24 (create→published), FR-25 (edit), FR-26 (soft delete), FR-27 (list + search)
- Admin app is a separate Vite project under `/admin` (not `/frontend`)
- TDesign Vue (`tdesign-vue-next`) for all UI components (table, form, dialog, etc.)
- JWT stored in `localStorage`; 401 response → redirect to `/admin/login`
- Admin routes all prefixed `/admin/`; public frontend routes unaffected

## Subtasks & Detailed Guidance

### Subtask T099 – Admin App Scaffold + Auth

**Purpose**: Initialize admin Vite app with TDesign Vue and JWT authentication.

**Steps**:
1. Create `/admin` with `npm create vite@latest . -- --template vue-ts`.
2. Install: `tdesign-vue-next`, `vue-router@4`, `pinia`, `axios`.
3. `src/main.ts`: register TDesign Vue globally + router + pinia.
4. `src/api/client.ts`: axios instance with:
   - `baseURL: import.meta.env.VITE_API_BASE_URL`
   - Request interceptor: add `Authorization: Bearer {token}` from localStorage
   - Response interceptor: on 401 → `router.push('/admin/login')`
5. `src/pages/LoginPage.vue`:
   - TDesign `t-form` with email + password fields
   - `POST /api/v1/admin/auth/login` → store token in `localStorage.setItem('admin_token', token)`
   - On success: redirect to `/admin/dashboard`
6. Route guard: `router.beforeEach` — redirect to `/admin/login` if no token.

**Files**:
- `admin/package.json`
- `admin/src/main.ts`
- `admin/src/api/client.ts`
- `admin/src/pages/LoginPage.vue`
- `admin/src/router/index.ts`

**Validation**:
- [ ] Login with valid credentials stores token and redirects to dashboard
- [ ] Accessing `/admin/dashboard` without token redirects to login
- [ ] 401 API response clears token and redirects to login

---

### Subtask T100 – Admin Layout + Dashboard

**Purpose**: Admin shell layout with sidebar navigation and stats dashboard.

**Steps**:
1. `src/layouts/AdminLayout.vue`:
   - TDesign `t-layout` with `t-aside` sidebar + `t-content` main area
   - Sidebar nav links: 仪表盘, 电影管理, 剧集管理, 动漫管理, 人物管理, 系列管理, 待审内容, 横幅管理
   - Top bar: site name + logout button (clears token + redirects to login)
2. `src/pages/DashboardPage.vue`:
   - Fetch `GET /api/v1/admin/stats`
   - 5 stat cards using TDesign `t-card`: 电影({n}), 剧集({n}), 动漫({n}), 人物({n}), 待审({n})
   - Pending count card highlighted in yellow if > 0

**Files**:
- `admin/src/layouts/AdminLayout.vue`
- `admin/src/pages/DashboardPage.vue`

**Validation**:
- [ ] Dashboard shows 5 stat cards with counts from API
- [ ] Pending count > 0 shows yellow highlight
- [ ] Logout clears token and redirects to login

---

### Subtask T101 – Movie List Page (Admin)

**Purpose**: Admin movie list with search, soft-delete toggle, and actions.

**Steps**:
1. `src/pages/movies/MovieListPage.vue`:
   - TDesign `t-table` with columns: ID, 标题, 年份, 评分, 状态, 创建时间, 操作
   - Search bar: `t-input` with debounce 500ms → `?q=` param
   - "显示已删除" toggle (`t-switch`) → `?include_deleted=true`
   - Soft-deleted rows: show with strikethrough + red "已删除" badge
   - Actions column: 编辑 button → `/admin/movies/{id}/edit`; 删除 button → confirm dialog → `DELETE /admin/movies/{id}`
   - Pagination via TDesign `t-pagination`
2. After delete: refresh list; show success toast.

**Files**:
- `admin/src/pages/movies/MovieListPage.vue`

**Validation**:
- [ ] Search by keyword filters results
- [ ] "显示已删除" toggle shows soft-deleted rows
- [ ] Delete button shows confirm dialog; on confirm calls DELETE API
- [ ] Soft-deleted rows shown with strikethrough

---

### Subtask T102 – Movie Create/Edit Form

**Purpose**: Admin form for creating and editing movies with credits management.

**Steps**:
1. `src/pages/movies/MovieFormPage.vue` (used for both create and edit):
   - Route: `/admin/movies/new` and `/admin/movies/:id/edit`
   - TDesign `t-form` with fields:
     - `title_cn` (required), `title_original`, `title_aliases` (tag input)
     - `synopsis` (textarea), `genres` (multi-select), `region`, `language`
     - `release_dates` (dynamic list: date + region pairs)
     - `douban_score`, `imdb_score`, `duration_min`
     - `poster_cos_key` (file upload via `POST /admin/upload` → returns cos_key)
     - `status` (select: published/draft)
   - Credits section: dynamic list of credit rows (person search + role + character name + display order)
     - Person search: autocomplete input calling `GET /admin/people?q=`
   - Submit: `POST /admin/movies` (create) or `PUT /admin/movies/:id` (edit)
   - Validation errors from API (422): display field-level errors using TDesign form validation
2. Image upload: click poster area → file input → `POST /admin/upload` → set `poster_cos_key`.

**Files**:
- `admin/src/pages/movies/MovieFormPage.vue`

**Validation**:
- [ ] Create form submits and redirects to movie list with success toast
- [ ] Edit form pre-fills all fields from `GET /admin/movies/:id`
- [ ] 422 validation errors shown on correct fields
- [ ] Poster upload works; cos_key stored in form

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| TDesign form validation vs API 422 errors | Map API `errors` object to TDesign form field error messages |
| Credits person search debounce | 300ms debounce on person autocomplete input |
| JWT expiry during long edit session | 401 interceptor redirects to login; form data lost (acceptable for admin) |

## Review Guidance

- Admin app is completely separate from public frontend (`/admin` vs `/frontend`)
- Movie create always sets `status='published'` (no draft workflow for admin-created content)
- Credits: full replace on update (not merge) — UI should show current credits pre-filled for edit

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
