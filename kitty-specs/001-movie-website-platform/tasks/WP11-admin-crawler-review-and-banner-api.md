---
work_package_id: "WP11"
subtasks:
  - "T045"
  - "T046"
  - "T047"
  - "T048"
  - "T049"
title: "Admin API – Crawler Review + Banner CRUD"
phase: "Phase 2 - Admin API"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP10"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Prompt generated via /spec-kitty.tasks"
---

# Work Package Prompt: WP11 – Admin API – Crawler Review + Banner CRUD

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP11 --base WP10
```

---

## Objectives & Success Criteria

- `GET /api/v1/admin/pending` lists crawler-submitted content awaiting review; `GET /api/v1/admin/pending/:id` returns full detail
- `POST /api/v1/admin/pending/:id/approve` creates the content entity (movie/tv/anime) from pending data and marks pending as `approved`
- `POST /api/v1/admin/pending/:id/reject` marks pending as `rejected` with reason; `POST /admin/pending/:id/reset` returns to `pending` status
- `POST /api/v1/admin/pending/bulk-approve` approves multiple pending items in one request
- Full CRUD for `featured_banners` table; `PUT /admin/banners/order` reorders banners

## Context & Constraints

- **Spec**: FR-28 (review queue), FR-29 (approve/reject), FR-30 (bulk approve), FR-31 (banner management)
- `pending_content` table: `id`, `content_type`, `raw_data` (JSONB), `review_status` (pending/approved/rejected), `submitted_at`, `reviewed_at`, `reviewer_note`
- On approve: deserialize `raw_data` JSONB → create Movie/TVSeries/Anime via existing admin application service (reuse T040–T042 commands)
- `featured_banners`: `id`, `title`, `subtitle`, `image_cos_key`, `link_url`, `display_order`, `is_active`, `start_date`, `end_date`
- All endpoints require `[Authorize]`

## Subtasks & Detailed Guidance

### Subtask T045 – GET /admin/pending List + Detail

**Purpose**: List pending crawler submissions with filtering; detail view for review.

**Steps**:
1. Add `AdminPendingController.cs` with `[Route("api/v1/admin/pending")]`.
2. `GET /admin/pending` with query params: `?status=pending|approved|rejected` (default: `pending`), `?content_type=movie|tv_series|anime`, `?page=`, `?page_size=20`.
3. `GET /admin/pending/:id` returns full `raw_data` JSONB deserialized as `object` (pass-through to frontend for pre-fill form).
4. `PendingListItemDto`:
   ```csharp
   public record PendingListItemDto(
       long Id, string ContentType, string? TitleCn,
       string ReviewStatus, DateTime SubmittedAt,
       DateTime? ReviewedAt, string? ReviewerNote
   );
   ```
   Extract `title_cn` from `raw_data->>'title_cn'` for display.

**Files**:
- `api/src/API/Controllers/Admin/AdminPendingController.cs`
- `api/src/Application/Admin/AdminPendingApplicationService.cs`

**Validation**:
- [ ] `GET /admin/pending` returns only `status='pending'` items by default
- [ ] `GET /admin/pending?status=approved` returns approved items
- [ ] `GET /admin/pending/:id` returns full `raw_data` object

---

### Subtask T046 – POST /admin/pending/:id/approve + Pre-fill

**Purpose**: Approve a pending submission by creating the actual content entity.

**Steps**:
1. `[HttpPost("{id:long}/approve")]` action.
2. `AdminPendingApplicationService.ApproveAsync(long pendingId, ct)`:
   a. Fetch `pending_content` by id; validate `review_status == 'pending'`.
   b. Deserialize `raw_data` to appropriate command DTO based on `content_type`:
      - `movie` → `CreateMovieCommand`
      - `tv_series` → `CreateTvSeriesCommand`
      - `anime` → `CreateAnimeCommand`
   c. Dispatch command via existing application service (reuse WP10 logic).
   d. Update `pending_content`: `review_status='approved'`, `reviewed_at=NOW()`.
   e. Return `{content_type, content_id}` of newly created entity.
3. Pre-fill endpoint: `GET /admin/pending/:id/prefill` — returns `raw_data` mapped to the edit form DTO shape (same as approve but read-only; admin can edit before approving).

**Files**:
- `api/src/Application/Admin/AdminPendingApplicationService.cs` (ApproveAsync)

**Validation**:
- [ ] `POST /admin/pending/5/approve` creates movie entity; pending row has `review_status='approved'`
- [ ] Approving already-approved item returns 409 Conflict
- [ ] `GET /admin/pending/5/prefill` returns structured DTO matching create form

---

### Subtask T047 – POST /admin/pending/:id/reject + /reset

**Purpose**: Reject a pending item with reason; reset rejected item back to pending.

**Steps**:
1. `[HttpPost("{id:long}/reject")]` with body `{reason: string}`:
   - Set `review_status='rejected'`, `reviewer_note=reason`, `reviewed_at=NOW()`.
   - Validate `reason` is non-empty (FluentValidation).
2. `[HttpPost("{id:long}/reset")]`:
   - Set `review_status='pending'`, clear `reviewer_note`, clear `reviewed_at`.
   - Only allowed if current status is `rejected` (return 409 if already pending/approved).

**Files**:
- `api/src/Application/Admin/AdminPendingApplicationService.cs` (RejectAsync, ResetAsync)

**Validation**:
- [ ] `POST /admin/pending/5/reject` with empty reason → 422
- [ ] `POST /admin/pending/5/reject` with reason → status becomes `rejected`
- [ ] `POST /admin/pending/5/reset` → status returns to `pending`

---

### Subtask T048 – POST /admin/pending/bulk-approve

**Purpose**: Approve multiple pending items in a single request.

**Steps**:
1. `[HttpPost("bulk-approve")]` with body `{ids: long[]}`.
2. `AdminPendingApplicationService.BulkApproveAsync(long[] ids, ct)`:
   a. Validate `ids` is non-empty, max 50 items.
   b. For each id: call `ApproveAsync` within a loop (each approval is its own UoW transaction).
   c. Collect results: `{approved: int, failed: [{id, error}]}`.
   d. Return partial success (200) even if some fail — report per-item status.
3. Do NOT wrap all approvals in a single transaction — partial success is acceptable per spec.

**Files**:
- `api/src/Application/Admin/AdminPendingApplicationService.cs` (BulkApproveAsync)

**Validation**:
- [ ] `POST /admin/pending/bulk-approve` with `{ids:[1,2,3]}` approves all 3
- [ ] If id=2 fails (already approved), response shows `{approved:2, failed:[{id:2, error:"..."}]}`
- [ ] Max 50 ids enforced; 51 ids → 422

---

### Subtask T049 – Featured Banner CRUD

**Purpose**: Full CRUD for homepage featured banners with display order management.

**Steps**:
1. `AdminBannersController.cs`:
   - `GET /admin/banners` — list all banners ordered by `display_order ASC`
   - `POST /admin/banners` — create banner
   - `PUT /admin/banners/:id` — update banner
   - `DELETE /admin/banners/:id` — hard delete (banners are not soft-deleted)
   - `PUT /admin/banners/order` — accepts `List<{id, display_order}>` and bulk-updates `display_order`
2. `CreateBannerCommand` DTO: `title` (required), `subtitle`, `image_cos_key` (required), `link_url`, `display_order` (int), `is_active` (bool), `start_date`, `end_date`.
3. After any banner change: invalidate Redis key `home:featured_banners`.
4. `PUT /admin/banners/order` body: `[{id: 1, display_order: 1}, {id: 2, display_order: 2}]` — bulk UPDATE in single query.

**Files**:
- `api/src/API/Controllers/Admin/AdminBannersController.cs`
- `api/src/Application/Admin/AdminBannersApplicationService.cs`

**Validation**:
- [ ] `POST /admin/banners` with missing `image_cos_key` → 422
- [ ] `PUT /admin/banners/order` reorders banners; `GET /admin/banners` reflects new order
- [ ] After banner update, Redis `home:featured_banners` key is deleted

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| `raw_data` JSONB may not match current command DTO shape | Use lenient deserialization (ignore unknown fields); log warnings for missing required fields |
| Bulk approve with 50 items may be slow | Each approval is independent; run sequentially with early exit on critical errors |
| Banner order gaps after delete | Re-normalize display_order on delete (or accept gaps — frontend sorts by display_order) |

## Review Guidance

- Approve: creates real entity + marks pending approved; does NOT delete pending row
- Reject reason is mandatory (not optional)
- Bulk approve: partial success is OK; return per-item status
- Banner hard delete (no soft delete); cache invalidation required after any change

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
