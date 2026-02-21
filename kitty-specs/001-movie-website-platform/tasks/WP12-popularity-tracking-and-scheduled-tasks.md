---
work_package_id: WP12
title: Popularity Tracking + Scheduled Tasks
lane: planned
dependencies:
- WP04
subtasks:
- T050
- T051
- T052
phase: Phase 2 - Extended Backend API
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

# Work Package Prompt: WP12 – Popularity Tracking + Scheduled Tasks

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP12 --base WP03
```

---

## Objectives & Success Criteria

- Every public content detail page view increments `popularity` counter in Redis (buffered, not direct DB write)
- A background job flushes Redis popularity increments to PostgreSQL every 5 minutes
- A daily cron job at 02:00 refreshes all 7 ranking lists in Redis (hot + score for movie/tv/anime + Top100)
- `IHostedService` implementations registered in DI; no external scheduler dependency

## Context & Constraints

- **Spec**: FR-36 (popularity tracking), FR-37 (daily ranking refresh)
- Popularity increment: `INCR popularity:movie:{id}` in Redis on each detail page view
- Flush job: reads all `popularity:*` keys, batch-updates `movies.popularity` / `tv_series.popularity` / `anime.popularity` in DB, then deletes the Redis keys
- Rankings refresh: re-runs the 7 ranking queries from WP09 T039 and writes results to Redis with 24h TTL
- Use `IHostedService` + `PeriodicTimer` (.NET 6+) for both jobs

## Subtasks & Detailed Guidance

### Subtask T050 – Popularity Increment on Page View

**Purpose**: Record a view event for a content item without blocking the API response.

**Steps**:
1. Add `IPopularityService` interface with `RecordViewAsync(string contentType, long id)`.
2. Implementation `RedisPopularityService`:
   ```csharp
   public async Task RecordViewAsync(string contentType, long id, CancellationToken ct = default)
   {
       var key = $"popularity:{contentType}:{id}";
       await _redis.StringIncrementAsync(key);
   }
   ```
3. Call `RecordViewAsync` from detail endpoints (MovieController, TvController, AnimeController) — fire-and-forget (do NOT await; wrap in `_ = Task.Run(...)`):
   ```csharp
   _ = _popularityService.RecordViewAsync("movie", id);
   ```
4. Register `RedisPopularityService` as scoped in DI.

**Files**:
- `api/src/Application/Common/IPopularityService.cs`
- `api/src/Infrastructure/Services/RedisPopularityService.cs`

**Validation**:
- [ ] `GET /api/v1/movies/123` increments `popularity:movie:123` in Redis
- [ ] Redis key is integer type (INCR); multiple views accumulate

---

### Subtask T051 – Popularity Flush Background Job

**Purpose**: Periodically flush Redis popularity counters to PostgreSQL.

**Steps**:
1. Create `PopularityFlushJob : BackgroundService`:
   ```csharp
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
       using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
       while (await timer.WaitForNextTickAsync(stoppingToken))
           await FlushAsync(stoppingToken);
   }
   ```
2. `FlushAsync`:
   a. Scan Redis for keys matching `popularity:*:*` using `IServer.KeysAsync("popularity:*")`.
   b. For each key, `GET` value and `DEL` atomically (use Lua script or pipeline):
      ```lua
      local val = redis.call('GET', KEYS[1])
      redis.call('DEL', KEYS[1])
      return val
      ```
   c. Parse `contentType` and `id` from key pattern `popularity:{type}:{id}`.
   d. Batch UPDATE: `UPDATE {table} SET popularity = popularity + @delta WHERE id = @id`.
   e. Use SqlSugar bulk update or raw SQL for efficiency.
3. Register in `Program.cs`: `builder.Services.AddHostedService<PopularityFlushJob>()`.

**Files**:
- `api/src/Infrastructure/BackgroundJobs/PopularityFlushJob.cs`

**Validation**:
- [ ] After 5 minutes, `movies.popularity` incremented by accumulated Redis count
- [ ] Redis keys deleted after flush (no double-counting)
- [ ] Job handles empty Redis (no keys) gracefully

---

### Subtask T052 – Daily Rankings Refresh Cron Job

**Purpose**: Refresh all 7 ranking lists in Redis daily at 02:00.

**Steps**:
1. Create `RankingsRefreshJob : BackgroundService`:
   ```csharp
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
       while (!stoppingToken.IsCancellationRequested)
       {
           var now = DateTime.UtcNow;
           var next2am = now.Date.AddDays(now.Hour >= 18 ? 1 : 0).AddHours(18); // 02:00 CST = 18:00 UTC
           await Task.Delay(next2am - now, stoppingToken);
           await _rankingsService.RefreshAllRankingsAsync(stoppingToken);
       }
   }
   ```
2. `RankingsApplicationService.RefreshAllRankingsAsync()`:
   - Re-run all 7 ranking queries (hot movie/tv/anime, score movie/tv/anime, Top100).
   - Write each result to Redis with 24h TTL (overwrite existing keys).
   - Log completion with item counts per list.
3. Register: `builder.Services.AddHostedService<RankingsRefreshJob>()`.
4. Also expose `POST /api/v1/admin/rankings/refresh` (admin-only) to trigger manual refresh.

**Files**:
- `api/src/Infrastructure/BackgroundJobs/RankingsRefreshJob.cs`
- `api/src/API/Controllers/Admin/AdminController.cs` (add manual refresh action)

**Validation**:
- [ ] After `POST /admin/rankings/refresh`, all 7 Redis ranking keys updated
- [ ] Rankings TTL reset to 24h after refresh
- [ ] Job calculates correct next-run time (02:00 CST)

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Redis SCAN on large keyspace is slow | Use `SCAN` with `COUNT 100` hint; acceptable for 5-min interval |
| Popularity flush race condition (key deleted between GET and DEL) | Use Lua script for atomic GET+DEL |
| Rankings refresh at 02:00 may overlap with high traffic | Rankings are read-heavy; overwriting Redis keys is atomic; no DB lock needed |

## Review Guidance

- Popularity: fire-and-forget (never blocks API response); Redis INCR is atomic
- Flush: GET+DEL must be atomic (Lua script); batch DB update preferred over row-by-row
- Rankings refresh: overwrites Redis keys (not append); 24h TTL reset on each refresh

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
