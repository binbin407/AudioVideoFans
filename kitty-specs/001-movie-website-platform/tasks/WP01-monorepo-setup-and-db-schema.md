---
work_package_id: WP01
title: Monorepo Setup & Database Schema
lane: "done"
dependencies: []
base_branch: master
base_commit: 5af857fdf54659905def32e53cc98804677805e0
created_at: '2026-02-23T01:14:34.318698+00:00'
subtasks:
- T001
- T002
- T003
- T004
- T005
- T006
phase: Phase 0 - Infrastructure Foundation
assignee: ''
agent: "codex"
shell_pid: "41800"
review_status: "has_feedback"
reviewed_by: "binbin407"
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP01 – Monorepo Setup & Database Schema

## ⚠️ IMPORTANT: Review Feedback Status

**Read this first if you are implementing this task!**

- **Has review feedback?**: Check the `review_status` field above. If it says `has_feedback`, scroll to the **Review Feedback** section immediately.
- **You must address all feedback** before your work is complete.

---

## Review Feedback

**Reviewed by**: binbin407
**Status**: ❌ Changes Requested
**Date**: 2026-02-23

**Issue 1（已修复，已验证）**

- 文件：`api/migrations/000_extensions.sql`
- 结论：此前关于 `pg_ts_token_type(...)` 的兼容性问题已修复，当前实现改为 PostgreSQL 15 可执行的安全写法（先确保配置存在，再执行 mapping）。

---

**Issue 2（High，仍需修复）**

- 文件：`kitty-specs/001-movie-website-platform/tasks/WP01-monorepo-setup-and-db-schema.md`
- 现象：`master..HEAD` 评审差异仍包含任务元数据漂移（`lane/shell_pid/history` 等非交付实现内容），导致本次评审 diff 不仅包含 WP01 交付代码。

**How to fix**

1. 将 WP01 分支同步到最新 `master` 后，清理该任务文件在功能分支中的非交付改动，确保 `git diff master..HEAD --stat` 仅包含 WP01 实现文件（monorepo skeleton + `api/migrations/000-004.sql` + `api/README.md` 等）。
2. 重新检查：
   - `git log master..HEAD --oneline`
   - `git diff master..HEAD --stat`

---

**Issue 3（High，仍需修复）**

- 文件：`kitty-specs/001-movie-website-platform/tasks/WP02-dotnet-ddd-scaffold.md`
- 现象：本次 `master..HEAD` 差异中把 `WP02` 的状态从 `done/approved` 回退为 `planned`（含 `lane/agent/shell_pid/review_status/reviewed_by` 和 activity log 回退），属于跨 WP 状态污染，不应出现在 WP01 交付分支中。

**How to fix**

1. 将 `WP02-dotnet-ddd-scaffold.md` 恢复为与当前 `master` 一致。
2. 确认 WP01 分支不包含任何其他 WP 的状态/元数据改动。

---

**Dependency/coordination note（required）**

- Dependents: `WP02`, `WP13`。
- 若 WP01 后续再次改动并重提审，请通知依赖方在继续前 rebase：
  - `cd .worktrees/001-movie-website-platform-WP02 && git rebase 001-movie-website-platform-WP01`
  - `cd .worktrees/001-movie-website-platform-WP13 && git rebase 001-movie-website-platform-WP01`

---

**Dependency checks（review）**

- dependency_check：WP01 frontmatter `dependencies: []`，无前置阻塞。
- dependent_check：存在依赖方 `WP02`（当前 `lane: done`）、`WP13`（当前 `lane: planned`）。
- verify_instruction：依赖声明与当前代码耦合一致（WP01 提供 monorepo 基线 + 数据库迁移基础；下游 WP 在此基础上继续实现）。


## Implementation Command

```bash
spec-kitty implement WP01
```

---

## Objectives & Success Criteria

- Initialize the 4-subsystem monorepo structure (`/frontend`, `/admin`, `/api`, `/crawler`) with placeholder files
- Create all 18 PostgreSQL tables with correct columns, constraints, indexes, and FTS generated columns
- Configure zhparser full-text search extension for Chinese text
- All migrations apply cleanly on a fresh PostgreSQL 15 instance
- `\dt` in psql lists: movies, tv_series, anime, tv_seasons, tv_episodes, anime_seasons, anime_episodes, people, credits, franchises, keywords, content_keywords, media_videos, award_events, award_ceremonies, award_nominations, featured_banners, pending_content, page_views

## Context & Constraints

- **Spec**: `kitty-specs/001-movie-website-platform/spec.md`
- **Plan**: `kitty-specs/001-movie-website-platform/plan.md`
- **Data Model**: `kitty-specs/001-movie-website-platform/data-model.md` — the authoritative SQL source
- **Quickstart**: `kitty-specs/001-movie-website-platform/quickstart.md` — local dev setup
- Tech stack: PostgreSQL 15+, .NET Core 10 (EF Core for migrations), zhparser extension
- All entity tables require `deleted_at TIMESTAMPTZ` for soft-delete (FR-26)
- `search_vector` is a STORED generated column — database maintains it automatically (no triggers needed)
- `page_views` table is needed by WP12 (popularity scoring) — must be created here

## Subtasks & Detailed Guidance

### Subtask T001 – Initialize Monorepo Directory Structure

**Purpose**: Create the project root with the 4 sub-project directories and minimal stub files so all teams can start in parallel.

**Steps**:
1. At the repository root, create these directories:
   ```
   /frontend/        # Vue 3 + Vite + Tailwind CSS
   /admin/           # Vue 3 + Vite + TDesign Vue
   /api/             # .NET Core 10 Web API
   /crawler/         # Python 3.11 + Scrapy
   ```
2. Add stub placeholder files to each (e.g., `frontend/README.md`, `api/README.md`):
   ```
   README.md (root) — monorepo overview, links to subsystem READMEs
   .gitignore — cover all 4 languages: C#, Node, Python
   .editorconfig — 4-space indent for C#, 2-space for TS/Python
   ```
3. Create `docker-compose.yml` stub (to be filled in WP26): just `version: '3.9'` with comments for each service.
4. Create `api/` .NET solution stub: `dotnet new sln -n MovieSite` (actual project creation in WP02).

**Files**:
- `/README.md` (new)
- `/.gitignore` (new — include: `bin/`, `obj/`, `node_modules/`, `.env`, `__pycache__/`, `.venv/`)
- `/.editorconfig` (new)
- `/docker-compose.yml` (stub)

**Validation**:
- [ ] `git status` shows 4 new directories
- [ ] `.gitignore` covers `node_modules/`, `*.user`, `.env`, `__pycache__/`

---

### Subtask T002 – Migration: Core Content Tables (movies, tv_series, anime)

**Purpose**: Create the three primary content tables with all columns, check constraints, indexes, and FTS generated columns as defined in `data-model.md`.

**Steps**:
1. In `api/`, create the first EF Core migration or raw SQL file `api/migrations/001_core_content_tables.sql`:
2. Create `franchises` table first (referenced by movies FK):
   ```sql
   CREATE TABLE franchises (
     id BIGSERIAL PRIMARY KEY,
     name_cn VARCHAR(200) NOT NULL,
     name_en VARCHAR(200),
     overview TEXT,
     poster_cos_key VARCHAR(500),
     deleted_at TIMESTAMPTZ,
     created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
     updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
   );
   ```
3. Create `movies` table with all columns from `data-model.md` (see the full definition there — includes JSONB columns, TEXT[] arrays, generated `search_vector` column, franchise FK, all indexes).
4. Create `tv_series` table with TV-specific fields (`air_status`, `first_air_date`, `next_episode_info` JSONB, `number_of_seasons`, etc.).
5. Create `anime` table with anime-specific fields (`origin VARCHAR(10)`, `source_material VARCHAR(30)`, `studio VARCHAR(200)`) plus all check constraints.

**Key details**:
- `movies.release_dates`: `JSONB DEFAULT '[]'` — stores array of `{region, date, type}` objects
- `movies.douban_rating_dist`: `JSONB` — stores `{five, four, three, two, one}` percentages
- `search_vector` for all 3 tables: `GENERATED ALWAYS AS (setweight(...) || ...) STORED`
- Use `zhparser` configuration name `chinese_zh` in generated column expressions (created in T006)

**⚠️ Order matters**: Create `franchises` before `movies` (FK dependency).

**Files**:
- `api/migrations/001_core_content_tables.sql` (new, ~120 lines)

**Validation**:
- [ ] `psql -c "\dt movies"` shows the table with correct columns
- [ ] INSERT a test row; verify `search_vector` column is populated by DB

---

### Subtask T003 – Migration: Season and Episode Tables

**Purpose**: Create season and episode tables for both TV series and anime.

**Steps**:
1. Create `api/migrations/002_season_episode_tables.sql`:
2. `tv_seasons` table: `id`, `series_id BIGINT REFERENCES tv_series(id) ON DELETE CASCADE`, `season_number INTEGER`, `name`, `episode_count`, `first_air_date`, `poster_cos_key`, `overview`, `vote_average DECIMAL(3,1)`, timestamps; UNIQUE constraint on `(series_id, season_number)`.
3. `tv_episodes` table: `id`, `season_id BIGINT REFERENCES tv_seasons(id) ON DELETE CASCADE`, `episode_number INTEGER`, `name`, `air_date`, `overview`, `duration_min`, `still_cos_key`, `vote_average`; UNIQUE on `(season_id, episode_number)`.
4. `anime_seasons` table: identical structure to tv_seasons but FK references `anime(id)`.
5. `anime_episodes` table: identical structure to tv_episodes but FK references `anime_seasons(id)`.
6. Indexes:
   ```sql
   CREATE INDEX idx_tv_seasons_series ON tv_seasons(series_id, season_number);
   CREATE INDEX idx_tv_episodes_season ON tv_episodes(season_id, episode_number);
   CREATE INDEX idx_anime_seasons_anime ON anime_seasons(anime_id, season_number);
   CREATE INDEX idx_anime_episodes_season ON anime_episodes(season_id, episode_number);
   ```

**Files**:
- `api/migrations/002_season_episode_tables.sql` (new, ~60 lines)

**Validation**:
- [ ] Can INSERT a tv_season with season_number=1 for a valid series_id
- [ ] Duplicate (series_id, season_number) INSERT raises unique violation

---

### Subtask T004 – Migration: People, Credits, Keywords

**Purpose**: Create the people, credits (polymorphic), keywords, and content_keywords tables.

**Steps**:
1. Create `api/migrations/003_people_credits_keywords.sql`:
2. `people` table: all fields from `data-model.md` including `photos_cos_keys TEXT[] DEFAULT '{}'`, `family_members JSONB DEFAULT '[]'`, `search_vector` generated column.
3. `credits` table (polymorphic): `person_id FK→people`, `content_type VARCHAR(20)` (CHECK IN 'movie','tv_series','anime'), `content_id BIGINT`, `role VARCHAR(50)`, `department`, `character_name`, `display_order`; index on `(content_type, content_id)` and `(person_id)`.
4. `keywords` table: `id BIGSERIAL PRIMARY KEY`, `name VARCHAR(100) NOT NULL UNIQUE`.
5. `content_keywords` table (polymorphic): `keyword_id FK→keywords`, `content_type`, `content_id`; PRIMARY KEY `(keyword_id, content_type, content_id)`; index on `(content_type, content_id)`.

**Files**:
- `api/migrations/003_people_credits_keywords.sql` (new, ~70 lines)

**Validation**:
- [ ] `credits` table accepts a row for each content_type value
- [ ] `content_keywords` PRIMARY KEY prevents duplicate associations

---

### Subtask T005 – Migration: Supporting Tables

**Purpose**: Create media_videos, award tables (3-level hierarchy), featured_banners, pending_content, and page_views tables.

**Steps**:
1. Create `api/migrations/004_supporting_tables.sql`:
2. `media_videos`: polymorphic `(content_type, content_id)`, `url VARCHAR(1000)`, `type` with CHECK constraint on valid video types, `published_at DATE`.
3. Award hierarchy:
   - `award_events`: `id`, `name_cn`, `name_en`, `slug VARCHAR(100) UNIQUE`, `description`, `official_url`
   - `award_ceremonies`: `id`, `event_id FK→award_events`, `edition_number INTEGER`, `year INTEGER`, `ceremony_date DATE`; UNIQUE on `(event_id, edition_number)`
   - `award_nominations`: `id`, `ceremony_id FK→award_ceremonies`, `category`, `content_type`, `content_id`, `person_id FK→people NULL`, `is_winner BOOLEAN DEFAULT FALSE`, `note`
4. `featured_banners`: `id`, `content_type` (CHECK IN 'movie','tv_series','anime'), `content_id BIGINT`, `display_order INTEGER DEFAULT 0`, `start_at TIMESTAMPTZ`, `end_at TIMESTAMPTZ`; partial index for active banners.
5. `pending_content`: `id`, `source VARCHAR(20)` (CHECK IN 'douban','mtime','tmdb'), `source_url VARCHAR(1000) UNIQUE`, `content_type`, `raw_data JSONB NOT NULL`, `review_status VARCHAR(20) DEFAULT 'pending'`, `reviewed_at`, `created_at`.
6. `page_views`: `id BIGSERIAL PRIMARY KEY`, `content_type VARCHAR(20) NOT NULL`, `content_id BIGINT NOT NULL`, `viewed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`; composite index `(content_type, content_id, viewed_at)`.

**Files**:
- `api/migrations/004_supporting_tables.sql` (new, ~80 lines)

**Validation**:
- [ ] All 6 tables exist; pending_content rejects duplicate `source_url`
- [ ] `featured_banners` partial index is created without errors

---

### Subtask T006 – Configure zhparser Full-Text Search

**Purpose**: Install and configure the zhparser PostgreSQL extension for Chinese text tokenization, enabling the `search_vector` generated columns to work.

**Steps**:
1. Create `api/migrations/000_extensions.sql` (runs before all other migrations):
   ```sql
   CREATE EXTENSION IF NOT EXISTS zhparser;
   CREATE EXTENSION IF NOT EXISTS pg_trgm;  -- ILIKE fallback support

   CREATE TEXT SEARCH CONFIGURATION chinese_zh (PARSER = zhparser);
   ALTER TEXT SEARCH CONFIGURATION chinese_zh
     ADD MAPPING FOR n, v, a, i, e, l, j, h, k, x WITH simple;
   ```
2. Verify the configuration is active: `SELECT cfgname FROM pg_ts_config WHERE cfgname = 'chinese_zh';`
3. Test a tokenization: `SELECT to_tsvector('chinese_zh', '星际穿越是一部科幻电影');` — should return tokens.
4. Document in `api/README.md`: zhparser installation steps for PostgreSQL 15 (compile from source or Docker image). Reference: https://github.com/amutu/zhparser

**⚠️ Critical**: The `search_vector` generated columns in movies/tv_series/anime/people ALL reference `chinese_zh` text search configuration. This migration MUST run FIRST before T002–T005.

**Fallback strategy**: If zhparser is unavailable, `pg_trgm` extension allows `ILIKE '%query%'` searches to use GIN trigram indexes. This is the API-level fallback (WP09), but the DB schema is the same either way.

**Files**:
- `api/migrations/000_extensions.sql` (new)
- `api/README.md` (update with zhparser install instructions)

**Validation**:
- [ ] `SELECT cfgname FROM pg_ts_config WHERE cfgname = 'chinese_zh';` returns 1 row
- [ ] After applying all migrations, `\dt` lists all 18+ tables
- [ ] `SELECT search_vector FROM movies WHERE id = 1;` returns non-null tsvector after test insert

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| zhparser not available on standard PostgreSQL 15 Docker image | Build custom Docker image FROM postgres:15, compile zhparser from source; document in README |
| Generated column expression fails if zhparser not installed when migration runs | Run `000_extensions.sql` first; add explicit dependency ordering in migration runner |
| `release_dates` JSONB date extraction for release-year filtering | Use `jsonb_array_elements(release_dates)->>'date'` in API layer (WP04), not at schema level |
| `tv_series` and `anime` table names conflict with reserved words | Both are fine as table names in PostgreSQL; no reserved word conflicts |

## Review Guidance

- Verify all 18+ tables are created by running `\dt` in psql
- Check that `search_vector` column exists on movies, tv_series, anime, people tables
- Verify indexes are present: `\di movies*` should show 7+ indexes
- Confirm `pending_content.source_url` has a UNIQUE constraint
- Test that inserting a row with duplicate `source_url` fails with unique violation

## Activity Log

> **CRITICAL**: Activity log entries MUST be in chronological order (oldest first, newest last).

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
- 2026-02-23T01:14:37Z – claude – shell_pid=25052 – lane=doing – Assigned agent via workflow command
- 2026-02-23T01:41:22Z – claude – shell_pid=25052 – lane=for_review – Ready for review: monorepo skeleton, PostgreSQL migrations (000-004), zhparser setup docs
- 2026-02-23T01:45:40Z – claude – shell_pid=6792 – lane=doing – Started review via workflow command
- 2026-02-23T01:49:12Z – claude – shell_pid=6792 – lane=planned – Moved to planned
- 2026-02-23T01:53:50Z – claude – shell_pid=6792 – lane=for_review – Ready for review: fixed PG15-safe zhparser extension migration and rebased onto latest master
- 2026-02-23T01:54:50Z – claude – shell_pid=17788 – lane=doing – Started review via workflow command
- 2026-02-23T02:07:07Z – claude – shell_pid=17788 – lane=planned – Moved to planned
- 2026-02-23T06:12:54Z – codex – shell_pid=32356 – lane=doing – Started review via workflow command
- 2026-02-23T06:19:24Z – codex – shell_pid=32356 – lane=planned – Moved to planned
- 2026-02-23T06:20:48Z – codex – shell_pid=41800 – lane=doing – Started review via workflow command
- 2026-02-23T06:28:31Z – codex – shell_pid=41800 – lane=done – Review passed: cleaned metadata drift; diff now contains only WP01 deliverables
