---
work_package_id: "WP25"
subtasks:
  - "T107"
  - "T108"
  - "T109"
  - "T110"
title: "Admin Crawler Review + Banner Management"
phase: "Phase 5 - Admin Frontend"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP23"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Prompt generated via /spec-kitty.tasks"
---

# Work Package Prompt: WP25 – Admin Crawler Review + Banner Management

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP25 --base WP23
```

---

## Objectives & Success Criteria

- Pending content review queue: list with status filter, bulk-select + bulk-approve
- Single item review: pre-filled edit form from `raw_data`; approve/reject with reason
- Banner management: list with active/inactive toggle, create/edit form, drag-and-drop order
- Dashboard pending count badge updates after approvals

## Context & Constraints

- **Spec**: FR-28 (review queue), FR-29 (approve/reject), FR-30 (bulk approve), FR-31 (banner management)
- Pending list: default filter `status=pending`; tabs for pending/approved/rejected
- Bulk approve: checkbox selection + "批量通过" button; max 50 items
- Banner order: drag-and-drop same pattern as franchise movie order (WP24)
- After approve/reject: refresh list and update dashboard stats count

## Subtasks & Detailed Guidance

### Subtask T107 – Pending Content Review List

**Purpose**: Review queue list with status tabs and bulk selection.

**Steps**:
1. `src/pages/pending/PendingListPage.vue`:
   - Status tabs: 待审核({count}) / 已通过 / 已拒绝
   - TDesign `t-table` with checkbox column for bulk selection
   - Columns: ID, 内容类型, 标题(from raw_data), 提交时间, 状态, 操作
   - "批量通过" button (enabled when ≥1 selected): confirm dialog → `POST /admin/pending/bulk-approve`
   - Per-row actions: 审核(→ detail page) / 通过(quick approve) / 拒绝(inline reason input)
2. After bulk approve: deselect all; refresh list; show toast with `{approved}通过, {failed}失败`.

**Files**:
- `admin/src/pages/pending/PendingListPage.vue`

**Validation**:
- [ ] Default tab shows pending items only
- [ ] Selecting 3 items + "批量通过" calls bulk-approve with 3 IDs
- [ ] After approve, item moves to "已通过" tab

---

### Subtask T108 – Pending Item Review Form

**Purpose**: Pre-filled review form for approving/editing crawler-submitted content.

**Steps**:
1. `src/pages/pending/PendingReviewPage.vue`:
   - Route: `/admin/pending/:id`
   - Fetch `GET /admin/pending/:id` to get `raw_data`
   - Render appropriate form based on `content_type`:
     - `movie` → MovieFormPage fields pre-filled from `raw_data`
     - `tv_series` → TvFormPage fields
     - `anime` → AnimeFormPage fields
   - Two action buttons: "通过并创建" (approve) + "拒绝" (reject with reason)
   - "通过并创建": submit edited form data → `POST /admin/pending/:id/approve`
   - "拒绝": show reason textarea → `POST /admin/pending/:id/reject`
2. Reuse form components from WP23/WP24 (pass `initialData` prop).

**Files**:
- `admin/src/pages/pending/PendingReviewPage.vue`

**Validation**:
- [ ] Form pre-filled with crawler data (title, score, genres, etc.)
- [ ] Approving creates content entity; pending status → approved
- [ ] Rejecting without reason shows validation error
- [ ] After action: redirect to pending list

---

### Subtask T109 – Banner Management

**Purpose**: Featured banner CRUD with active toggle and display order management.

**Steps**:
1. `src/pages/banners/BannerListPage.vue`:
   - TDesign `t-table` with columns: 预览图, 标题, 状态, 展示顺序, 有效期, 操作
   - Active toggle: TDesign `t-switch` → `PUT /admin/banners/:id` with `{is_active: bool}`
   - Drag-and-drop reorder (same `vuedraggable` pattern as franchise) → "保存排序" → `PUT /admin/banners/order`
   - 编辑/删除 actions
2. `src/pages/banners/BannerFormPage.vue`:
   - Fields: `title` (required), `subtitle`, `image_cos_key` (upload), `link_url`, `is_active`, `start_date`, `end_date`, `display_order`
   - Image preview: show uploaded banner image (full-width preview, 16:6 aspect ratio)

**Files**:
- `admin/src/pages/banners/BannerListPage.vue`
- `admin/src/pages/banners/BannerFormPage.vue`

**Validation**:
- [ ] Active toggle immediately updates banner status
- [ ] Drag reorder + "保存排序" persists new order
- [ ] Banner image preview shown in form

---

### Subtask T110 – Dashboard Stats Refresh

**Purpose**: Keep dashboard pending count in sync after review actions.

**Steps**:
1. Use Pinia store `src/stores/stats.ts`:
   ```typescript
   export const useStatsStore = defineStore('stats', {
     state: () => ({ pending: 0, movies: 0, tvSeries: 0, anime: 0, people: 0 }),
     actions: {
       async refresh() {
         const data = await getAdminStats()
         Object.assign(this, data)
       }
     }
   })
   ```
2. Dashboard page: call `statsStore.refresh()` on mount.
3. After any approve/reject action: call `statsStore.refresh()`.
4. Sidebar nav: show pending count badge next to "待审内容" link using `statsStore.pending`.

**Files**:
- `admin/src/stores/stats.ts`

**Validation**:
- [ ] Pending count in sidebar badge decrements after approval
- [ ] Dashboard stats refresh after approve/reject
- [ ] Stats store shared between dashboard and sidebar

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Pre-fill form from raw_data: field name mismatches | Normalize raw_data keys in `PendingReviewPage` before passing to form |
| Bulk approve partial failure UX | Show per-item result in toast: "3通过, 1失败(ID:42: 已存在)" |
| Banner active toggle race condition | Disable toggle during API call; re-enable after response |

## Review Guidance

- Pending review: admin can edit crawler data before approving (not just approve as-is)
- Reject reason is mandatory (API enforces; UI must also validate)
- Stats refresh: call after every approve/reject to keep sidebar badge accurate

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
