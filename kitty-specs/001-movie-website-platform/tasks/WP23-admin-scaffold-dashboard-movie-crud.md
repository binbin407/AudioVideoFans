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

## Design Reference

**Source**: `design/design.pen` — frames `vkhWP` (Admin Dashboard), `cIdBD` (Movie List), `eUXet` (Edit Form)

### Admin Layout

**Sidebar** (frame `sidebar` in `vkhWP`):
```
width: 260px
fill: #111114
border-right: 1px solid #2A2A2E
padding: [24, 16]
layout: vertical
gap: 8
```
- Logo row: `#FF8400` movie icon (20px) + "影视网管理" text `fontSize: 16, fontWeight: 700, color: #FFFFFF`, `gap: 8px`, `padding: [0, 8]`, `margin-bottom: 24px`
- Nav item: `padding: [10, 12]`, `cornerRadius: 8`, `gap: 12px`, `fontSize: 14`
  - Default: `color: #B8B9B6`, `fill: transparent`
  - Active: `color: #FF8400`, `fill: #FF840015`
  - Hover: `fill: #1E1E22`
- Nav icons: Material Symbols Rounded, 20px
- Section divider: `fill: #2A2A2E`, `height: 1px`, `margin: 8px 0`

**Main Content Area**:
```
fill: #0B0B0E
layout: vertical
padding: [24, 32]
gap: 24
```

**Page Header**:
- Title: `fontSize: 22, fontWeight: 700, color: #FFFFFF`
- Subtitle/breadcrumb: `fontSize: 13, color: #6B6B70`

### Dashboard Stats Cards (frame `statsRow` in `vkhWP`)

4 cards in a row, `gap: 16px`:
```
cornerRadius: 12
fill: #16161A
border: 1px solid #2A2A2E
padding: 20
layout: vertical
gap: 8
```
- Label: `fontSize: 13, color: #6B6B70`
- Value: `fontSize: 28, fontWeight: 700, color: #FFFFFF`
- Change indicator: `fontSize: 12` — positive `color: #22c55e`, negative `color: #ef4444`
- Icon: Material Symbols Rounded, 24px, `color: #FF8400`, top-right corner

### Movie List Page (frame `cIdBD`)

**Toolbar** (above table):
```
justify-content: space-between
gap: 16px
```
- Left: search input (`width: 280px`, `cornerRadius: 8`, `fill: #16161A`, `border: 1px solid #2A2A2E`) + status filter dropdown + type filter dropdown
- Right: "新增电影" button (`fill: #FF8400`, `color: #FFFFFF`, `cornerRadius: 8`, `padding: [8, 16]`)

**Table** (TDesign `t-table`):
```
cornerRadius: 12
fill: #16161A
border: 1px solid #2A2A2E
```
- Header row: `fill: #111114`, `color: #6B6B70`, `fontSize: 13`
- Data row: `fill: #16161A`, `color: #FFFFFF`, `fontSize: 14`
- Row hover: `fill: #1E1E22`
- Row border: `border-bottom: 1px solid #2A2A2E`
- Columns: 海报(60px) / 标题 / 年份 / 评分 / 状态 / 操作
- Status badge: `cornerRadius: 4`, `padding: [2, 8]`, `fontSize: 12`
  - published: `fill: #22c55e20`, `color: #22c55e`
  - draft: `fill: #6B6B7020`, `color: #6B6B70`
  - deleted: `fill: #ef444420`, `color: #ef4444`
- Action buttons: "编辑" `color: #FF8400` + "删除" `color: #ef4444`, `fontSize: 13`

### Movie Edit Form (frame `eUXet`)

**Form Layout**: single column, `gap: 24px`, `max-width: 800px`

Form sections (TDesign `t-card` with title):
1. **基本信息**: 中文名 / 原名 / 年份 / 时长 / 地区 / 语言
2. **分类信息**: 类型(多选) / 关键词(tag input)
3. **剧情简介**: textarea, `min-height: 120px`
4. **媒体资源**: 海报上传 + 背景图上传 (COS upload)
5. **演职人员**: 动态列表 — 搜索人物 + 选择角色类型 + 角色名

Form field style (TDesign overrides):
- Input/Select: `fill: #16161A`, `border: 1px solid #2A2A2E`, `color: #FFFFFF`
- Label: `fontSize: 14, color: #B8B9B6`
- Required mark: `color: #FF8400`
- Error state: `border-color: #ef4444`

**Form Actions** (sticky bottom bar):
- "保存草稿" button: `fill: #16161A`, `border: 1px solid #2A2A2E`, `color: #FFFFFF`
- "发布" button: `fill: #FF8400`, `color: #FFFFFF`
- Gap: `16px`

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
