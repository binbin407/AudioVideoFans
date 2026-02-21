# Work Packages: 影视资讯网站平台

**Inputs**: Design documents from `kitty-specs/001-movie-website-platform/`
**Prerequisites**: plan.md ✓, spec.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: No explicit test WPs included (not requested). Tests are embedded as validation steps within each WP where critical.

**Organization**: 115 fine-grained subtasks (T001–T115) grouped into 26 work packages (WP01–WP26). Each work package is independently deliverable in one focused session.

---

## Phase 0: Infrastructure Foundation

---

## Work Package WP01: Monorepo Setup & Database Schema (Priority: P0) 🎯 Foundation

**Goal**: Initialize the 4-subsystem monorepo structure and create all PostgreSQL database tables, indexes, and full-text search configuration.
**Independent Test**: All migrations apply cleanly on a fresh PostgreSQL 15 instance with zhparser installed. `\dt` lists all 18 tables with correct column counts.
**Prompt**: `tasks/WP01-monorepo-setup-and-db-schema.md`
**Estimated Size**: ~380 lines

### Included Subtasks
- [ ] T001 Initialize monorepo directory structure (`/frontend`, `/admin`, `/api`, `/crawler`, root CI config stubs)
- [ ] T002 Write PostgreSQL migration: core content tables (`movies`, `tv_series`, `anime` with all columns, constraints, indexes, FTS generated columns)
- [ ] T003 Write PostgreSQL migration: season/episode tables (`tv_seasons`, `tv_episodes`, `anime_seasons`, `anime_episodes`)
- [ ] T004 Write PostgreSQL migration: people, credits, franchises, keywords, content_keywords
- [ ] T005 Write PostgreSQL migration: media_videos, award_events/ceremonies/nominations, featured_banners, pending_content, page_views
- [ ] T006 Configure zhparser FTS: install extensions, create `chinese_zh` TEXT SEARCH CONFIGURATION, verify `search_vector` generated columns work

### Implementation Notes
- Use EF Core Code-First migrations in `api/` for .NET-managed schema; raw SQL for FTS configuration
- All entity tables include `deleted_at TIMESTAMPTZ` for soft-delete
- `page_views` table (content_type, content_id, viewed_at) is required for popularity scoring (WP12)
- `search_vector` is a STORED generated column — no triggers needed

### Parallel Opportunities
- T001 (monorepo scaffold) can be done while T002–T006 SQL is being written

### Dependencies
- None (starting package)

### Risks & Mitigations
- zhparser install may fail on some PostgreSQL Docker images → use `pgsql/postgresql:15` with extensions pre-built, document fallback
- Generated column syntax differs slightly between PostgreSQL versions → test on 15.x specifically

---

## Work Package WP02: .NET Core 10 DDD Backend Scaffold (Priority: P0)

**Goal**: Create the .NET Core 10 solution with DDD 4-layer architecture, all domain entities, repository interfaces, and base SqlSugar infrastructure.
**Independent Test**: `dotnet build` succeeds; empty API starts at `https://localhost:5001`; DI resolves all registered services without errors.
**Prompt**: `tasks/WP02-dotnet-ddd-scaffold.md`
**Estimated Size**: ~360 lines

### Included Subtasks
- [ ] T007 Create .NET solution structure: Domain / Application / Infrastructure / API projects with project references
- [ ] T008 Define all Domain entities with SqlSugar attributes: Movie, TvSeries, Anime, TvSeason, TvEpisode, AnimeSeason, AnimeEpisode, Person, Credit, Franchise, Keyword, ContentKeyword, MediaVideo, AwardEvent, AwardCeremony, AwardNomination, FeaturedBanner, PendingContent
- [ ] T009 Define IRepository\<T\> generic interface + specialized interfaces (IMovieRepository, ITvSeriesRepository, etc.) in Domain; implement SqlSugarRepository\<T\> base class in Infrastructure
- [ ] T010 Configure SqlSugar DI (Scoped ISqlSugarClient, PgSqlIsAutoToLower = false), implement IUnitOfWork + UnitOfWork
- [ ] T011 Scaffold Application layer: command/query handler structure (no MediatR needed; direct service classes), base DTOs for all content types

### Implementation Notes
- `PgSqlIsAutoToLower = false` is critical — without it SqlSugar will map column names to lowercase breaking PostgreSQL column resolution
- TEXT[] columns: use `[SugarColumn(ColumnDataType = "text[]")]`; JSONB: use `IsJson = true`
- Generated columns (`search_vector`): mark with `IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true`
- PostgreSQL array filters (`&&` operator) must use raw SQL — SqlSugar cannot translate these

### Parallel Opportunities
- T007 (solution structure) can start immediately; T008 entity definitions can be distributed per developer if multiple

### Dependencies
- Depends on WP01 (DB schema defines column structure)

### Risks & Mitigations
- SqlSugar JSONB mapping requires exact `ColumnDataType` — test round-trip serialize/deserialize for each JSONB column

---

## Work Package WP03: Backend Infrastructure Services (Priority: P0)

**Goal**: Implement Redis caching layer, Tencent COS storage client, global middleware (exception handling, request logging, CORS, Swagger), and OAuth 2.0 JWT authentication.
**Independent Test**: `GET /api/v1/health` returns 200; `GET /api/admin/stats` without token returns 401; Swagger UI loads at `/swagger`.
**Prompt**: `tasks/WP03-backend-infrastructure-services.md`
**Estimated Size**: ~320 lines

### Included Subtasks
- [ ] T012 Redis cache service: implement `IRedisCache` (Get/Set/Delete/Exists), define `CacheKeys` constants class matching naming spec (e.g., `movie:detail:{id}`, `movies:list:{hash}`)
- [ ] T013 Tencent COS storage client: `ITencentCosClient` interface + implementation for upload/delete; `CosUrlHelper.GetCdnUrl(cosKey)` utility
- [ ] T014 Global middleware: `GlobalExceptionMiddleware` (catch-all → structured error JSON), `RequestLoggingMiddleware` (method, path, status, duration), CORS policy, Swagger/OpenAPI with JWT bearer
- [ ] T015 OAuth 2.0 JWT RS256 authentication: configure `AddAuthentication().AddJwtBearer()` with JWKS URI; `[Authorize]` on all `/api/admin/**` controllers
- [ ] T016 Sentry SDK integration: `UseSentry()` in Program.cs; Prometheus /metrics endpoint via `prometheus-net.AspNetCore`; verify error capture in development mode

### Implementation Notes
- Redis key hashing for list queries: `MD5(JsonSerializer.Serialize(filterDto))` → short deterministic cache key
- Cache invalidation: Application Layer services must call `_redis.Delete(CacheKeys.MovieDetail(id))` after any save/update
- COS client uses `COSSTS.NET` SDK or direct HTTP — store only COS object key in DB, never full URLs

### Parallel Opportunities
- T012 (Redis) and T013 (COS) are independent and can be done in parallel

### Dependencies
- Depends on WP02 (solution structure must exist)

### Risks & Mitigations
- Sentry DSN must be empty string in development (not null) to avoid initialization errors → use `if (!string.IsNullOrEmpty(dsn))` guard

---

## Phase 1: Core Backend API (Movie + Home)

---

## Work Package WP04: Home + Movie List API (Priority: P1) 🎯 MVP

**Goal**: Implement the home page aggregation endpoint and the movie list endpoint with all filtering/sorting/pagination dimensions.
**Independent Test**: `GET /api/v1/home` returns Banner + hot lists with ≥1 item each (given seed data). `GET /api/v1/movies?genre=sci-fi&region=us&decade=2020s&sort=douban_score&page=1` returns filtered results with correct `pagination` object.
**Prompt**: `tasks/WP04-home-and-movie-list-api.md`
**Estimated Size**: ~310 lines

### Included Subtasks
- [ ] T017 `GET /api/v1/home`: HomeController + HomeApplicationService — fetch active FeaturedBanners (start_at/end_at time filter), hot movies (top 8 by popularity), hot TV (top 8), hot anime by origin (国漫 top 8, 日漫 top 8); Redis cache `home:banners` 10min
- [ ] T018 `GET /api/v1/movies`: MovieListQuery with filter params (genre, region, decade, year, lang, score threshold, sort, page, page_size); PostgreSQL array `&&` filter using raw SQL for genres/region/language; Redis cache `movies:list:{hash}` 10min
- [ ] T019 Array filter SQL helper: `ArrayFilterHelper.BuildWhereClause(filters)` generates parameterized SQL fragments for TEXT[] overlap filters; decade-to-year-range conversion (2020s → 2020–2029)
- [ ] T020 Cache invalidation strategy: after any Movie create/update, Application Layer calls `_redis.DeletePattern("movies:list:*")` and `_redis.Delete(CacheKeys.MovieDetail(id))`

### Implementation Notes
- Home endpoint must respect `start_at ≤ NOW() ≤ end_at` (or NULL bounds) for banners
- When `douban_score IS NULL`, exclude from score-filtered results (not treated as 0)
- Sort options: `douban_score DESC NULLS LAST`, `popularity DESC`, `release_date DESC NULLS LAST` (use earliest CN release date from `release_dates` JSONB)
- Each list response uses `MediaCardDto` (id, title_cn, year, poster_cos_key, douban_score, genres)

### Parallel Opportunities
- T017 (home) and T018 (movie list) can be built in parallel

### Dependencies
- Depends on WP02 (entities), WP03 (Redis, middleware)

### Risks & Mitigations
- JSONB array filter for `release_dates` year extraction requires PostgreSQL JSONB operators — use `jsonb_array_elements(release_dates)->>'date'` in raw SQL
- `DeletePattern` for Redis requires SCAN command — implement carefully to avoid blocking production

---

## Work Package WP05: Movie Detail API (Priority: P1) 🎯 MVP

**Goal**: Implement full movie detail endpoint including credits, similar content, and franchise detail page API.
**Independent Test**: `GET /api/v1/movies/1` returns all sections (franchise block null-safe, awards empty array acceptable). `GET /api/v1/movies/1/similar` returns ≤6 items ordered by keyword overlap.
**Prompt**: `tasks/WP05-movie-detail-api.md`
**Estimated Size**: ~300 lines

### Included Subtasks
- [ ] T021 `GET /api/v1/movies/:id`: assemble MovieDetailDto — base fields + franchise (with order/total), cast (top 20 by display_order), directors, awards (all nominations for this movie across ceremonies), videos (by type), similar (6), extra_posters/backdrops; Redis cache `movie:detail:{id}` 1h
- [ ] T022 `GET /api/v1/movies/:id/credits`: full paginated credits grouped by department (directors / writers / cast / producers / others); no Redis cache (infrequent access)
- [ ] T023 `SimilarContentService`: given (content_type, content_id), find up to 6 similar items — primary: JOIN content_keywords by overlap count DESC; fallback: genre array overlap; exclude self; respect soft-delete and status=published
- [ ] T024 `GET /api/v1/franchises/:id`: FranchiseDetailDto — franchise info + all movies with franchise_id, ordered by franchise_order ASC, including poster/title/year/douban_score

### Implementation Notes
- `MovieDetailDto.franchise` is null when `franchise_id IS NULL` — do not include empty object
- Awards: JOIN award_nominations → award_ceremonies → award_events; include `is_winner`, `category`, `event_cn`, `edition`
- Franchise total count: `SELECT COUNT(*) FROM movies WHERE franchise_id = :id AND deleted_at IS NULL`
- SimilarContentService must handle all 3 content types (movie/tv_series/anime)

### Parallel Opportunities
- T021–T024 can all be built in parallel (separate controllers/services)

### Dependencies
- Depends on WP04 (establishes API patterns + ArrayFilterHelper)

### Risks & Mitigations
- Keyword overlap query with LEFT JOIN + GROUP BY can be slow on large datasets → add EXPLAIN ANALYZE, ensure `idx_content_keywords_content` is used

---

## Work Package WP06: TV Series API (Priority: P2)

**Goal**: Implement TV series list, detail, season detail endpoints with all TV-specific filters (air_status) and season/episode data.
**Independent Test**: `GET /api/v1/tv?status=airing&genre=mystery` returns only airing series. `GET /api/v1/tv/456/seasons/3` returns season header + 10 episodes with S03E01 format.
**Prompt**: `tasks/WP06-tv-series-api.md`
**Estimated Size**: ~300 lines

### Included Subtasks
- [ ] T025 `GET /api/v1/tv`: TvSeriesListQuery — same filter dimensions as movies plus `air_status` multi-value filter (airing/ended/production/cancelled); sort by `first_air_date DESC` option; Redis cache `tv:list:{hash}` 10min
- [ ] T026 `GET /api/v1/tv/:id`: TvSeriesDetailDto — base info + `air_status` label + `next_episode_info` (only when airing and not null) + seasons summary (each season: poster, name, episode_count, first_air_date, vote_average, overview); Redis cache `tv:detail:{id}` 1h
- [ ] T027 `GET /api/v1/tv/:id/seasons/:season_number`: SeasonDetailDto — season header fields + full episode list ordered by episode_number ASC (each: id, episode_number, name, air_date, duration_min, still_cos_key, overview); include prev/next season numbers
- [ ] T028 `GET /api/v1/tv/:id/similar`: reuse SimilarContentService (content_type='tv_series')

### Implementation Notes
- `air_status` filter supports multiple values (`?status=airing&status=ended`); build `WHERE air_status = ANY(@statuses)` SQL
- Season detail page needs prev/next: query MIN and MAX season_number for the series, return adjacent
- `next_episode_info` JSON shape: `{air_date, title, season_number, episode_number}` — pass through from DB as-is

### Parallel Opportunities
- T025 (list) and T026–T028 (detail) are independent

### Dependencies
- Depends on WP02, WP03 (follows patterns from WP04/WP05)

---

## Work Package WP07: Anime API (Priority: P2)

**Goal**: Implement anime list, detail, and season detail endpoints with anime-specific filters (origin, source_material).
**Independent Test**: `GET /api/v1/anime?origin=cn&source=manga` returns only Chinese manga-adapted anime. `GET /api/v1/anime/789` includes studio, source_material, and origin fields.
**Prompt**: `tasks/WP07-anime-api.md`
**Estimated Size**: ~290 lines

### Included Subtasks
- [ ] T029 `GET /api/v1/anime`: AnimeListQuery — all standard filters + `origin` (cn/jp/other) + `source_material` (original/manga/novel/game); Redis cache `anime:list:{hash}` 10min
- [ ] T030 `GET /api/v1/anime/:id`: AnimeDetailDto — all base fields + `origin`, `studio`, `source_material` fields; seasons summary with voice_actor credits separated from other credits; Redis cache `anime:detail:{id}` 1h
- [ ] T031 `GET /api/v1/anime/:id/seasons/:season_number`: AnimeSeason detail with full episode list and prev/next season nav (same structure as TV season)
- [ ] T032 `GET /api/v1/anime/:id/similar`: reuse SimilarContentService (content_type='anime')

### Implementation Notes
- Voice actors (role='voice_actor') must be returned separately with `character_name` field in cast
- `AnimeDetailDto` adds `origin_label` computed field: cn→「国漫」, jp→「日漫」, other→「其他」
- Origin filter: `WHERE origin = @origin` (simple equality, not array overlap)

### Parallel Opportunities
- Can be built fully in parallel with WP06 (same patterns, different entity)

### Dependencies
- Depends on WP02, WP03

---

## Work Package WP08: People + Awards API (Priority: P2)

**Goal**: Implement person detail endpoint (including top-8 collaborator query) and the awards main page + edition detail endpoints.
**Independent Test**: `GET /api/v1/people/888` includes top-8 collaborators with correct count. `GET /api/v1/awards/oscar/96` returns nominations grouped by category with is_winner flag.
**Prompt**: `tasks/WP08-people-and-awards-api.md`
**Estimated Size**: ~310 lines

### Included Subtasks
- [ ] T033 `GET /api/v1/people/:id`: PersonDetailDto — profile fields (name_cn/en, professions, birth/death/place/nationality/height), biography, works list (all content types from credits, grouped by role), photos_cos_keys; Redis cache `person:detail:{id}` 1h
- [ ] T034 Collaborator query: for a given person_id, find top-8 co-workers — JOIN credits on content_type+content_id where another person also has a credit, GROUP BY co-person_id, ORDER BY co_count DESC LIMIT 8; include co-person avatar/name
- [ ] T035 `GET /api/v1/awards/:slug`: AwardEventDetailDto — event info (name_cn/en, description, official_url) + all ceremonies list (edition_number, year, ceremony_date)
- [ ] T036 `GET /api/v1/awards/:slug/:edition`: CeremonyDetailDto — ceremony info + nominations grouped by category; each nomination includes content info (poster, title) + person info + is_winner flag

### Implementation Notes
- Works list Tab filter: query parameter `role=actor|director|writer|all` — add to `/people/:id?role=actor` or return all roles and let frontend filter
- Collaborator query performance: `credits` table indexed on `(content_type, content_id)` and `person_id` — use those indexes
- Award nominations: join to movies/tv_series/anime for content info based on `content_type`

### Parallel Opportunities
- T033+T034 (people) fully parallel to T035+T036 (awards)

### Dependencies
- Depends on WP02, WP03

---

## Work Package WP09: Search + Rankings API (Priority: P2)

**Goal**: Implement full-text search with zhparser (ILIKE fallback), autocomplete, and hot/high-score rankings for all 3 content types.
**Independent Test**: `GET /api/v1/search?q=星际` returns results across all 4 types with counts. `GET /api/v1/search/autocomplete?q=星` returns grouped results in ≤100ms. `GET /api/v1/rankings` returns all 3 content type tabs with data.
**Prompt**: `tasks/WP09-search-and-rankings-api.md`
**Estimated Size**: ~270 lines

### Included Subtasks
- [ ] T037 `GET /api/v1/search?q=`: SearchQuery — try `search_vector @@ plainto_tsquery('chinese_zh', @q)` on all 4 tables; on error/no zhparser, fallback to `title_cn ILIKE '%@q%'`; aggregate results with per-type counts; support `type` filter param; sort by `ts_rank` DESC
- [ ] T038 `GET /api/v1/search/autocomplete?q=`: AutocompleteQuery — search top 3 per type (movie/tv_series/anime/people); Redis cache `search:autocomplete:{q}` 5min; return grouped response with `see_all_url`
- [ ] T039 `GET /api/v1/rankings`: RankingsQuery — hot榜 (popularity DESC, top 50 per type, daily Redis cache `rankings:{type}:hot` 24h); high-score榜 (douban_score DESC, top 50 per type, `rankings:{type}:score` 24h); Movie Top100 (douban_score ≥ 7.0 AND douban_rating_count ≥ 1000, top 100)

### Implementation Notes
- zhparser availability check: attempt extension query at startup, set `_zhparserAvailable` flag, use throughout service lifetime
- Full-text search across 4 tables requires UNION ALL — build union query with explicit content_type discriminator column
- Rankings Top100 gate: `WHERE douban_score >= 7.0 AND douban_rating_count >= 1000 AND deleted_at IS NULL AND status = 'published'`

### Dependencies
- Depends on WP02, WP03

---

## Phase 2: Admin API

---

## Work Package WP10: Admin API – Content CRUD (Priority: P3)

**Goal**: Implement admin endpoints for creating, updating, and soft-deleting all content types (Movie, TVSeries, Anime, Person, Franchise).
**Independent Test**: `POST /api/v1/admin/movies` with valid JWT and body creates a movie with status=published. `DELETE /api/v1/admin/movies/1` sets `deleted_at` instead of deleting the row.
**Prompt**: `tasks/WP10-admin-content-crud-api.md`
**Estimated Size**: ~370 lines

### Included Subtasks
- [ ] T040 Movie admin CRUD: `POST /admin/movies` (CreateMovieCommand → direct insert, status=published), `PUT /admin/movies/:id` (UpdateMovieCommand → update + invalidate Redis), `DELETE /admin/movies/:id` (SoftDeleteCommand → set deleted_at)
- [ ] T041 TVSeries admin CRUD: same pattern as movies + season/episode sub-resources (`POST /admin/tv/:id/seasons`, `POST /admin/tv/:id/seasons/:n/episodes`)
- [ ] T042 Anime admin CRUD: same pattern as TV + anime-specific fields (origin, studio, source_material)
- [ ] T043 Person + Franchise admin CRUD: `POST/PUT/DELETE /admin/people` (with photos_cos_keys array management), `POST/PUT/DELETE /admin/franchises`
- [ ] T044 `GET /api/v1/admin/stats`: count published/draft records per content type; title keyword search endpoint `GET /admin/{type}?q=keyword` for admin list pages

### Implementation Notes
- All admin endpoints require `[Authorize]` attribute — JWT RS256 validated against JWKS URI
- Credits management: when updating a movie, accept `credits[]` array in body; delete existing credits for content, re-insert new ones within UoW transaction
- Soft-delete: set `deleted_at = NOW()` only; admin GET endpoints must support `?include_deleted=true` param to show soft-deleted items
- Validation: use FluentValidation or DataAnnotations; return 422 with field-level errors

### Parallel Opportunities
- T040 (Movie), T041 (TV), T042 (Anime), T043 (Person/Franchise) are fully parallel

### Dependencies
- Depends on WP02, WP03

---

## Work Package WP11: Admin API – Crawler Review + Banner Management (Priority: P3)

**Goal**: Implement the pending_content review workflow (approve/reject/reset/bulk-approve) and Hero Banner CRUD.
**Independent Test**: `POST /admin/pending/1/approve` returns pre-filled DTO matching raw_data fields. `POST /admin/pending/bulk-approve` with `[1,2,3]` updates all 3 items within a single transaction.
**Prompt**: `tasks/WP11-admin-crawler-review-and-banner-api.md`
**Estimated Size**: ~350 lines

### Included Subtasks
- [ ] T045 `GET /api/v1/admin/pending`: list pending_content with `review_status` filter (pending/approved/rejected), paginated, sorted by created_at DESC; `GET /admin/pending/:id`: single item with raw_data formatted for display
- [ ] T046 `POST /admin/pending/:id/approve`: update review_status='approved' + reviewed_at; extract raw_data fields → map to entity DTO → return as `{prefilled_data: {...}, content_type}` so admin frontend can redirect to edit form
- [ ] T047 `POST /admin/pending/:id/reject`: set review_status='rejected'; `POST /admin/pending/:id/reset`: set review_status='pending', clear reviewed_at
- [ ] T048 `POST /admin/pending/bulk-approve`: accept `{ids: []}` body; loop approve within single UoW transaction; return `{approved_count, failed_ids}` response
- [ ] T049 Banner CRUD: `GET /admin/banners` (list with content_type/content_id, display_order, time range), `POST /admin/banners` (create with validation), `PUT /admin/banners/:id` (update order/times), `DELETE /admin/banners/:id` (hard delete for banner config, not content)

### Implementation Notes
- `approve` does NOT auto-publish content — it only returns pre-fill data. Admin must then submit the edit form (T040/T041/T042) to create the actual published record.
- raw_data → entity field mapping varies by content_type: create mapping dictionaries for douban/mtime/tmdb sources
- Banner `display_order`: allow gaps (e.g., 10, 20, 30) to ease reordering; frontend sorts by display_order ASC
- Banner active filter: `WHERE (start_at IS NULL OR start_at <= NOW()) AND (end_at IS NULL OR end_at > NOW())`

### Dependencies
- Depends on WP10 (admin patterns established)

---

## Work Package WP12: Popularity Tracking + Scheduled Tasks (Priority: P2)

**Goal**: Implement page view tracking endpoint, daily popularity score update job, and daily rankings cache refresh job.
**Independent Test**: `POST /api/v1/tracking/view` inserts a page_views record. After running the popularity cron, movie `popularity` field reflects last-7-day PV counts. Rankings Redis keys are refreshed after cron.
**Prompt**: `tasks/WP12-popularity-tracking-and-scheduled-tasks.md`
**Estimated Size**: ~240 lines

### Included Subtasks
- [ ] T050 `POST /api/v1/tracking/view`: fire-and-forget insert into `page_views (content_type, content_id, viewed_at)`; return 204 immediately; no authentication required; rate-limit by IP (max 10/min per content item to prevent abuse)
- [ ] T051 Daily popularity update job (cron: 02:30 daily): UPDATE movies/tv_series/anime/people SET popularity = (SELECT COUNT(*) FROM page_views WHERE content_type=X AND content_id=id AND viewed_at >= NOW()-7 DAYS); use IHostedService or Hangfire
- [ ] T052 Daily rankings cache refresh job (cron: 02:00 daily): regenerate and SET all `rankings:*:hot` and `rankings:*:score` Redis keys; ensures rankings reflect updated popularity and new content

### Implementation Notes
- Use .NET `BackgroundService` (IHostedService) for both cron jobs; use `NCrontab` or `Cronos` library for cron expression parsing
- page_views table: add composite index `(content_type, content_id, viewed_at)` for efficient 7-day windowed COUNT
- Popularity update should be done as a single bulk UPDATE per table, not row-by-row
- Rankings refresh: query + serialize to JSON + `SET key JSON EX 86400`

### Dependencies
- Depends on WP04 (API patterns), WP03 (Redis)

---

## Phase 3: Crawler

---

## Work Package WP13: Python Scrapy Crawler (Priority: P3)

**Goal**: Implement the full Scrapy crawler system with TMDB API spider, Douban HTML parser, Mtime sub-score parser, and the dedup + PostgreSQL write pipelines.
**Independent Test**: `scrapy crawl tmdb_spider -a content_type=movie -a ids=550` inserts 1 pending_content record. Re-running with same ID does not create duplicate (dedup by source_url).
**Prompt**: `tasks/WP13-scrapy-crawler.md`
**Estimated Size**: ~480 lines

### Included Subtasks
- [ ] T053 Scrapy project setup: `scrapy startproject crawler`; configure `settings.py` (DOWNLOAD_DELAY=3, RANDOMIZE_DOWNLOAD_DELAY=True, default headers, HTTPCACHE for dev); `requirements.txt` (scrapy, psycopg2-binary, python-dotenv)
- [ ] T054 Anti-crawl middleware: `proxy_middleware.py` (cycle through PROXY_LIST from settings, rotate per request), `useragent_middleware.py` (random UA from a pool of 20+ real browser UAs); enable in settings
- [ ] T055 Dedup pipeline: `dedup_pipeline.py` — check `pending_content.source_url` before insert; skip item if already exists (log SKIP); use psycopg2 direct connection for pipeline (not Django ORM)
- [ ] T056 PostgreSQL write pipeline: `postgres_pipeline.py` — INSERT INTO pending_content (source, source_url, content_type, raw_data) with conflict handling; close DB connection on spider_closed signal
- [ ] T057 TMDB spider: `tmdb_spider.py` — use TMDB API v3 (`/movie/{id}`, `/tv/{id}`); map TMDB response fields to `raw_data` schema matching `content_keywords`; handle pagination for bulk import via `ids` argument
- [ ] T058 Douban spider: `douban_spider.py` — parse Douban movie/TV/anime HTML; extract: title_cn, douban_score, douban_rating_count, douban_rating_dist (5-star distribution), synopsis; respect robots.txt
- [ ] T059 Mtime spider: `mtime_spider.py` — parse Mtime HTML for sub-scores (music/visual/director/story/performance); match to content by IMDB ID or title; store as `raw_data.mtime_scores`

### Implementation Notes
- TMDB spider uses official API (free key) — not HTML parsing; rate limit: 40 req/10s; use API v3 endpoints
- Douban + Mtime spiders do HTML parsing — DOWNLOAD_DELAY must be ≥ 3s with randomization to avoid blocks
- `raw_data` is JSONB — store full API/parsed response as-is; field mapping to entity fields happens during admin review (T046)
- Settings override: `settings_local.py` (gitignored) overrides `settings.py` for API keys + proxy list

### Parallel Opportunities
- T055+T056 (pipelines) can be built in parallel; T057, T058, T059 (spiders) fully parallel

### Dependencies
- Depends on WP01 (pending_content table must exist)

---

## Phase 4: Frontend – Common Components

---

## Work Package WP14: Frontend Scaffold + Common Components (Priority: P1) 🎯 MVP

**Goal**: Initialize the Vue 3 + Vite + Tailwind CSS frontend project and implement all shared UI components used across multiple pages.
**Independent Test**: `npm run dev` starts the dev server; `MediaCard` renders with fallback placeholder when `poster_cos_key` is null; `Lightbox` opens/closes with keyboard arrow navigation.
**Prompt**: `tasks/WP14-frontend-scaffold-and-common-components.md`
**Estimated Size**: ~420 lines

### Included Subtasks
- [ ] T060 Vue 3 + Vite project init: `npm create vue@latest frontend` (TypeScript strict, Vue Router, Pinia); install Tailwind CSS v4, Axios; create `src/api/` Axios client (base URL from env, error interceptor), `src/utils/cos.ts` (CDN URL helper), `src/stores/` Pinia setup
- [ ] T061 `MediaCard.vue`: 2:3 aspect ratio poster image (`object-cover`), title overlay, year badge, rating badge (hidden when `douban_score` is null — no placeholder text); fallback grey placeholder on image load error; click → router-link to detail page
- [ ] T062 `Pagination.vue`: prev/next buttons, up to 7 page number buttons with ellipsis for large ranges; emit `page-change` event; URL query param integration via `useRoute()`/`useRouter()`
- [ ] T063 `FilterBar.vue`: flat tag row layout (like Douban); each row has a label + tag buttons; selected tags highlighted with orange background; supports multi-select within row (configurable); emits `filter-change` with active selections; separate `DropdownFilter.vue` for language/score selects
- [ ] T064 `Lightbox.vue`: fullscreen overlay (fixed position, z-50, dark bg); image display with object-contain; left/right arrow buttons; keyboard `ArrowLeft`/`ArrowRight` navigation; `Escape` to close; prop: `images: string[]`, `initialIndex: number`; emit `close`
- [ ] T065 `ImageTabBlock.vue`: tabs array prop (name + cos_keys array + count); hide tab entirely when count=0 (not disabled); default active = first non-empty tab (「剧照」); images display as horizontal scroll row; click any image → open Lightbox at that index

### Implementation Notes
- Tailwind v4 configuration: use `@import "tailwindcss"` in main CSS; configure content paths
- `cosUrl()` helper: `${import.meta.env.VITE_COS_CDN_BASE}/${key}` — return null when key is null/empty
- MediaCard must NOT show any text in rating area when `douban_score` is null (spec requirement — no "X人想看" or placeholder)
- FilterBar tag rows: use CSS `flex-wrap` for natural wrapping on smaller screens

### Parallel Opportunities
- T061–T065 are all independent components, can be built in parallel

### Dependencies
- None (frontend starts independently of backend)

---

## Work Package WP15: Layout, NavBar, SearchBar & Core Composables (Priority: P1) 🎯 MVP

**Goal**: Implement the site layout shell (NavBar, Footer), expandable search bar with autocomplete, and the reusable composables (useFilters, useSearch, usePagination).
**Independent Test**: SearchBar displays autocomplete dropdown with grouped results when typing; pressing Enter navigates to `/search?q=...`; FilterBar state correctly serializes to/from URL query params.
**Prompt**: `tasks/WP15-layout-navbar-searchbar-composables.md`
**Estimated Size**: ~360 lines

### Included Subtasks
- [ ] T066 `NavBar.vue`: site logo (left), nav links (电影/电视剧/动漫/影人 → /movies, /tv, /anime, /people), search icon button (toggles search bar); responsive: collapse nav links into hamburger menu on mobile (`< 768px`); active link highlighting based on current route
- [ ] T067 `Footer.vue`: minimal footer with site name, brief description; responsive layout
- [ ] T068 `SearchBar.vue` (inline below NavBar when open): input field with debounce (300ms); call `GET /api/v1/search/autocomplete?q=`; display dropdown: 4 type sections (电影/电视剧/动漫/影人), max 3 results each, item shows poster thumbnail + title + year; 「查看全部结果」link at bottom; press Enter or click "查看全部" → navigate to `/search?q=`; click item → navigate to detail page; `useSearch` composable handles all fetch logic
- [ ] T069 `useFilters.ts` composable: reads URL query params on mount → reactive filter state object; watch filter state → `router.push()` with updated params; expose `activeFilters`, `setFilter(key, value)`, `clearFilters()`, `filterToQueryParams()`
- [ ] T070 `useSearch.ts` composable: debounced autocomplete fetch; `usePagination.ts`: page state from URL `?page=`, computed `totalPages`, `prevPage/nextPage` helpers

### Implementation Notes
- SearchBar autocomplete: close on click outside (use `onClickOutside` from VueUse or native blur handler)
- `useFilters` must handle array-valued filters (genre can be multi-select) — serialize as repeated params (`?genre=sci-fi&genre=action`)
- NavBar links use `router-link` with `exact-active-class` for proper highlighting

### Dependencies
- Depends on WP14 (project scaffold must exist)

---

## Phase 5: Frontend – Content Pages

---

## Work Package WP16: Home Page (Frontend) (Priority: P1) 🎯 MVP

**Goal**: Implement the home page with Hero Banner auto-carousel, horizontal scroll card lists for each content category, and the rankings/awards entry sections.
**Independent Test**: Banner auto-advances every 5 seconds; when banner list is empty, no banner section renders; hot movie cards show 8+ MediaCards in horizontal scroll without triggering page horizontal scrollbar at 1280px viewport.
**Prompt**: `tasks/WP16-home-page.md`
**Estimated Size**: ~290 lines

### Included Subtasks
- [ ] T071 Hero Banner carousel (`HeroBanner.vue`): fetch `/api/v1/home` → `banners`; auto-cycle every 5s with `setInterval`; manual dot indicator navigation; smooth transition (CSS transition or Vue `<Transition>`); do NOT render `<section>` when `banners.length === 0`; backdrop image fills full width with overlay gradient
- [ ] T072 Hot lists section: `HorizontalScroll.vue` wrapper (overflow-x auto, hide scrollbar on desktop, show on mobile); hot movies list (8+ MediaCards), hot TV list (8+ MediaCards); fetch data from `/api/v1/home`
- [ ] T073 Hot anime section with tabs: `国漫` / `日漫` toggle buttons; filter local state from combined home response; ≥8 cards per tab in horizontal scroll
- [ ] T074 Rankings entry cards (static grid: 「电影排行」「电视剧排行」「动漫排行」each linking to `/rankings?tab=movie|tv|anime`); Awards entry cards (奥斯卡、金球奖、戛纳等 image+text cards linking to `/awards/oscar` etc.)

### Implementation Notes
- Banner must clear interval on `onUnmounted()` to avoid memory leaks
- When home API call fails, show graceful empty state (no banner, no lists) — do not crash page
- Horizontal scroll: use `scroll-smooth` CSS, add left/right arrow buttons for desktop hover

### Dependencies
- Depends on WP14 (MediaCard, common components), WP15 (NavBar layout)

---

## Work Package WP17: Movie List Page (Frontend) (Priority: P1) 🎯 MVP

**Goal**: Implement the `/movies` list page with FilterBar (all dimensions), sort controls, paginated grid, and bidirectional URL ↔ filter state sync.
**Independent Test**: Selecting genre「科幻」+ region「欧美」updates URL to `?genre=sci-fi&region=us`; sharing that URL restores filter state on page load; no horizontal scrollbar at 1280px.
**Prompt**: `tasks/WP17-movie-list-page.md`
**Estimated Size**: ~300 lines

### Included Subtasks
- [ ] T075 `/movies/index.vue` structure: load filters from URL on mount via `useFilters()`; call `GET /api/v1/movies` with filter params; display `FilterBar` with rows: genre tags (科幻/动作/爱情/恐怖/喜剧/纪录片/动画/剧情/犯罪/悬疑...), region tags (大陆/香港/台湾/美国/英国/日本/韩国/法国...), decade tags (2020s/2010s/2000s/90s/更早)
- [ ] T076 Language dropdown + score dropdown filters (普通话/粤语/英语/日语/韩语/其他; 9分+/8分+/7分+/不限); sort controls row (综合热度/豆瓣评分/最新上映); results grid (3 cols mobile / 4 cols tablet / 6 cols desktop, 24 cards max)
- [ ] T077 URL sync: `useFilters()` composable (from WP15); on filter change → `router.replace()` with merged params (preserve page=1 on filter change); on page change → `router.push({query: {...currentFilters, page: n}})`
- [ ] T078 Pagination component placement below grid; loading skeleton state (show grey card placeholders during API fetch); empty state message when 0 results

### Implementation Notes
- FilterBar tag selection uses orange background for active tags (Tailwind: `bg-orange-500 text-white`)
- Multi-select within a row: genre, region, decade all support multi-select; language and score are single-select
- API call must debounce on filter change (300ms) to avoid rapid-fire requests during multi-filter selection

### Dependencies
- Depends on WP14 (MediaCard, FilterBar, Pagination), WP15 (useFilters)

---

## Work Package WP18: Movie Detail Page (Frontend) (Priority: P1) 🎯 MVP

**Goal**: Implement the `/movies/[id]` detail page with all sections: Hero, rating bars, cast/crew, trailers, awards, franchise block, similar content, and image tab block.
**Independent Test**: `<title>` is「复仇者联盟2 (2015) - 影视网」; franchise block not rendered when movie has no franchise; synopsis collapses at 150 chars with「展开」toggle; lightbox keyboard navigation works.
**Prompt**: `tasks/WP18-movie-detail-page.md`
**Estimated Size**: ~410 lines

### Included Subtasks
- [ ] T079 Hero section (`MovieDetailHero.vue`): backdrop image (full-width, blur filter overlay), poster (2:3 ratio), basic info (title_cn, title_original, year, genres, region, directors, main cast top-5); `<title>` tag via `useHead()` or direct `document.title`; `<meta name="description">` with synopsis excerpt
- [ ] T080 Ratings section (`RatingBlock.vue`): Douban score + 5-star distribution progress bars (力荐/推荐/还行/较差/很差 labels with percentages); IMDB score badge; Mtime sub-scores (music/visual/director/story/performance) — show only when data exists
- [ ] T081 Cast grid (clickable avatars → `/people/[id]`), Synopsis (`SynopsisBlock.vue` — collapse >150 chars, 「展开/收起」toggle), Videos section (tabs by type: 正式预告/花絮/幕后/片段 — embed iframe or YouTube link)
- [ ] T082 Awards block (`AwardBlock.vue` — gold icon for winners, grey for nominations, fold >5 with count link); Franchise block (`FranchiseBlock.vue` — render only when `franchise != null`, show series name link + 「第N部 / 共X部」); Similar content row (6 MediaCards, no render when empty array)
- [ ] T083 Image tab block (reuse `ImageTabBlock.vue` from WP14: 剧照 + 海报 tabs, default to 剧照, count in tab label, hide empty tabs); page-level layout and responsive breakpoints verification

### Implementation Notes
- Backdrop blur: `filter: blur(8px); transform: scale(1.1)` on bg image + dark overlay (`bg-black/60`) — do not use CSS backdrop-filter (Safari compat issues)
- `useHead()` or Vite plugin `vite-plugin-document-title` for `<title>` management in SPA mode
- Cast grid: show top 20 via `/movies/:id` endpoint data; "查看全部演职员" link → `/movies/:id/credits`

### Dependencies
- Depends on WP14 (ImageTabBlock, Lightbox, MediaCard), WP15 (layout)

---

## Work Package WP19: TV Series List + Detail Pages (Frontend) (Priority: P2)

**Goal**: Implement `/tv` list page (with air_status filter), `/tv/[id]` detail (with Next Episode block and Season Accordion), and `/tv/[id]/season/[n]` season detail page.
**Independent Test**: Season accordion defaults to latest season expanded; collapsing a season hides its episode list while keeping season header visible; next episode block only shows when series is airing with next_episode_info.
**Prompt**: `tasks/WP19-tv-series-pages.md`
**Estimated Size**: ~380 lines

### Included Subtasks
- [ ] T084 `/tv/index.vue`: TV list page — same FilterBar as movie list + additional air_status row (全部/连载中/已完结/制作中/已取消, multi-select); card shows air_status label badge (green for airing, grey for ended); all URL sync and sort controls
- [ ] T085 `/tv/[id].vue` Hero section + Next Episode block: Hero same pattern as movie detail; `NextEpisodeBlock.vue` — only render when `air_status === 'airing' && next_episode_info != null`; display predicted air date + episode title
- [ ] T086 `SeasonAccordion.vue`: accordion list of seasons; default expanded = highest season_number; folded state shows: season poster thumbnail (grey placeholder if null), season name, episode count, first_air_date, vote_average (when available), synopsis truncated to 3 lines (CSS `-webkit-line-clamp: 3`) with `…`; expanded state: poster + synopsis stay visible, episode list appears below; episode row: S{season_number}E{episode_number} code, title, air_date, duration, still_cos_key thumbnail
- [ ] T087 `/tv/[id]/season/[n].vue`: breadcrumb (剧名 → 第N季); season header (full non-truncated overview, poster, stats); full episode list ordered by episode_number; prev/next season navigation at page bottom (no prev if season 1, no next if latest)

### Implementation Notes
- Season accordion uses `v-show` (not `v-if`) for expanded content to avoid re-render flicker on toggle
- Breadcrumb: series name is a `<router-link>` to `/tv/:id`; no JavaScript needed for breadcrumb (static computed)
- Still image placeholder: grey `bg-gray-200` div with same aspect ratio as still images

### Dependencies
- Depends on WP14 (ImageTabBlock, MediaCard), WP15 (layout)

---

## Work Package WP20: Anime List + Detail Pages (Frontend) (Priority: P2)

**Goal**: Implement `/anime` list page (with origin tabs and source_material filter), `/anime/[id]` detail (with studio/origin blocks and voice actor credits), and `/anime/[id]/season/[n]`.
**Independent Test**: Clicking「国漫」tab updates URL `?origin=cn` and shows only cn-origin anime; voice actor cards show character name alongside actor name.
**Prompt**: `tasks/WP20-anime-pages.md`
**Estimated Size**: ~310 lines

### Included Subtasks
- [ ] T088 `/anime/index.vue`: origin tab row (全部/国漫/日漫 always visible at top, click → update `origin` query param); FilterBar: same base filters + separate source_material row (原创/漫画改编/小说改编/游戏改编); card badges show origin label + source_material label
- [ ] T089 `/anime/[id].vue`: Anime detail page — Hero section + production info block (`StudioBlock.vue`: studio name, source_material label, origin label 「国漫/日漫/其他」); credits section separates voice_actors (show character_name below actor name) from director/crew; SeasonAccordion reused; ImageTabBlock reused; Similar content
- [ ] T090 `/anime/[id]/season/[n].vue`: Anime season detail — identical structure to TV season detail page; breadcrumb links to `/anime/[id]`; `<title>` format: `{动漫名} 第{N}季 - 影视网`

### Implementation Notes
- `StudioBlock.vue`: display only when at least one of studio/source_material/origin has a value
- Voice actor card: two-line layout (top: actor name; bottom: 「配音：{character_name}」in smaller text)
- Reuse `SeasonAccordion.vue` from WP19 — pass seasons data from anime detail response

### Dependencies
- Depends on WP14, WP15, WP19 (SeasonAccordion component reuse)

---

## Work Package WP21: Person, Franchise & Awards Pages (Frontend) (Priority: P2)

**Goal**: Implement the person detail page (photo wall + collaborators), franchise detail page, and award main + edition detail pages.
**Independent Test**: Photo wall renders grid layout with lightbox; photo wall section hidden when `photos_cos_keys` is empty; franchise page movies sorted by franchise_order (序号 label visible); award nominations show gold highlight for winners.
**Prompt**: `tasks/WP21-person-franchise-awards-pages.md`
**Estimated Size**: ~360 lines

### Included Subtasks
- [ ] T091 `/people/[id].vue`: profile section (avatar, name_cn/en, professions tags, birth/death/nationality/height); biography collapsible >200 chars; works tab bar (全部/导演/编剧/演员) — each tab shows content cards with year + role info; for actor tab include character_name below title
- [ ] T092 `CollaboratorBlock.vue` (top-8 co-workers, each with avatar + name + 「合作N次」); `PhotoWall.vue` (grid layout `grid-cols-4 md:grid-cols-5`, lightbox on click, count in section title「写真 (N)」, entire section not rendered when array empty)
- [ ] T093 `/franchises/[id].vue`: page title (system name + 「共N部」); synopsis collapsible >200 chars; movie list ordered by franchise_order ASC, each entry: 「第N部」sequence label, poster, title, year, douban_score (「暂无评分」when null); click → `/movies/[id]`; `<title>` format: `{系列名} - 影视网`
- [ ] T094 `/awards/[slug].vue` (award main page: event info, link to editions) + `/awards/[slug]/[edition].vue` (nominations grouped by category: `<section>` per category, nominations list with poster + title link + person link + gold/grey is_winner icon); prev/next edition navigation

### Implementation Notes
- Works tab filter: client-side filter from full works list (not separate API calls per tab) — keep works data in Pinia store after initial fetch
- Photo wall grid: use CSS Grid, each cell `aspect-square` for uniform sizing; image object-fit: cover
- Award nomination sorting within category: winners first (is_winner=true at top), then nominations

### Dependencies
- Depends on WP14 (Lightbox, MediaCard), WP15 (layout)

---

## Work Package WP22: Search + Rankings + SEO (Frontend) (Priority: P2)

**Goal**: Implement the `/search` results page and `/rankings` page, plus site-wide SEO optimizations (title tags, meta descriptions, lazy loading).
**Independent Test**: Search `?q=星际` shows tabbed results with counts; the「电影」tab is active; tabs with 0 results are greyed out and unclickable; rankings page shows gold/silver/bronze badges for top 3.
**Prompt**: `tasks/WP22-search-rankings-seo.md`
**Estimated Size**: ~300 lines

### Included Subtasks
- [ ] T095 `/search/index.vue`: read `?q=` from URL; call `GET /api/v1/search?q=`; display tab bar (全部/电影/电视剧/动漫/影人 with count badges); inactive tabs (count=0) use `disabled` styling (grey, `cursor-not-allowed`); result card: poster thumbnail + title + year + type badge + synopsis first 60 chars; empty state: 「未找到与「{q}」相关的内容」
- [ ] T096 `/rankings/index.vue`: content-type tabs (电影/电视剧/动漫); sub-tabs (热门榜/高分榜); movie tab also shows Top100 entry; ranking list: rank badge (1=gold, 2=silver, 3=bronze; 4+=normal number), MediaCard style row with rank + poster + title + year + score; Top100 gate display note (「豆瓣评分 ≥ 7.0，评分人数 ≥ 1000」)
- [ ] T097 SEO optimizations: implement `usePageMeta(title, description)` composable that sets `document.title` and `<meta name="description">` on every page; ensure all detail pages call it with correct format strings from spec; add `sitemap.xml` generation script (optional)
- [ ] T098 Image performance: add `loading="lazy"` to all `<img>` in MediaCard, episode stills, photo wall; hero/banner images use `loading="eager"` + `fetchpriority="high"`; add fallback `onerror` handler to all images (show grey placeholder div)

### Implementation Notes
- Search tab active state: read `?type=movie|tv|anime|person` from URL + update on tab click
- Rankings page: combine hot榜 (from `popularity`) and 高分榜 (from `douban_score`) data in single `/rankings` API call
- Top100 badge: `<span class="badge">Top 100</span>` on movie high-score tab entry

### Dependencies
- Depends on WP14, WP15

---

## Phase 6: Admin Frontend

---

## Work Package WP23: Admin Frontend Scaffold + Dashboard + Movie CRUD (Priority: P3)

**Goal**: Initialize the admin Vue 3 + TDesign Vue project, implement OAuth 2.0 PKCE login flow, dashboard stats page, and the movie content create/edit/list pages.
**Independent Test**: Accessing `/admin` without token redirects to OAuth login; after login, dashboard shows content counts; movie create form validates required fields (title_cn) before submit.
**Prompt**: `tasks/WP23-admin-scaffold-dashboard-movie-crud.md`
**Estimated Size**: ~380 lines

### Included Subtasks
- [ ] T099 Admin Vue 3 + TDesign Vue project: `npm create vue@latest admin` (TypeScript); install `tdesign-vue-next`, Axios, Pinia; configure router with `beforeEach` auth guard (check JWT in localStorage → redirect to `/login` if missing); implement `/login` page with OAuth 2.0 PKCE flow (redirect to provider → callback → exchange code → store JWT)
- [ ] T100 Admin layout (`AdminLayout.vue`): TDesign `t-layout` with sidebar nav (内容管理/爬虫审核/Banner管理 sections) + header with logout button; Dashboard page (`/admin`): call `GET /admin/stats`, display `t-card` components with counts (电影 N 部 / 电视剧 N 部 / 动漫 N 部 / 影人 N 人 / 待审核 N 条)
- [ ] T101 Movie list page (`/admin/content/movies`): TDesign `t-table` with columns (ID, title_cn, status, created_at, actions); search by title input; soft-deleted toggle switch; 编辑/删除 action buttons per row; confirm dialog on delete
- [ ] T102 Movie create/edit form (`/admin/content/movies/new`, `/admin/content/movies/:id/edit`): all fields from MovieDetailDto; `t-form` with validation; credits section (searchable person select + role dropdown + character_name input, add/remove rows); franchise select (searchable); awards section (add/remove nomination records); submit → POST/PUT admin API

### Implementation Notes
- OAuth PKCE: generate `code_verifier` (random 43-128 chars), `code_challenge = BASE64URL(SHA256(code_verifier))`; store verifier in sessionStorage during redirect; exchange on callback
- JWT storage: use `localStorage` (or `sessionStorage` for stricter XSS mitigation); include in all API requests via Axios interceptor as `Authorization: Bearer {token}`
- Movie form credit management: fetch person by name search (`GET /admin/people?q=`) for autocomplete

### Dependencies
- None (admin frontend starts independently)

---

## Work Package WP24: Admin Frontend – TV/Anime/Person/Franchise CRUD (Priority: P3)

**Goal**: Implement admin CRUD pages for TV series (with season/episode management), Anime, Person, and Franchise.
**Independent Test**: Creating a TV series with 2 seasons and 5 episodes each persists correctly via API; person form uploads photos and shows preview; franchise form shows ordered movie list.
**Prompt**: `tasks/WP24-admin-tv-anime-person-franchise-crud.md`
**Estimated Size**: ~360 lines

### Included Subtasks
- [ ] T103 TV series list + create/edit forms: same list pattern as movies; edit form adds Season Management tab (add/edit/delete seasons + episodes inline); season row: season_number, name, first_air_date, poster upload; episode table: episode_number, name, air_date, duration, still upload; nested forms within main form
- [ ] T104 Anime list + create/edit forms: same as TV forms + anime-specific fields section (origin radio group: 国漫/日漫/其他; source_material select; studio text input); season/episode management identical to TV
- [ ] T105 Person list + create/edit forms: avatar upload (single image) + photos upload (multi-image gallery with preview and delete); professions checkboxes; family_members dynamic row (name + relation pairs); biography textarea; IMDB ID field
- [ ] T106 Franchise list + create/edit forms: name_cn/en, overview textarea; Associated movies section (list movies with franchise_id=this, reorder by drag or numeric input for franchise_order, add/remove association)

### Implementation Notes
- Image upload: upload to COS via admin API endpoint (`POST /admin/upload`) → receive cos_key → store in form field
- Season/episode nested forms: use TDesign `t-collapse` for season accordion; episode table is inline editable
- Franchise movie association: show movie search + current association list; franchise_order must be unique within a franchise

### Dependencies
- Depends on WP23 (admin scaffold + patterns established)

---

## Work Package WP25: Admin Frontend – Crawler Review + Banner Management (Priority: P3)

**Goal**: Implement the pending content review list + approve/reject flow, and the Hero Banner management page.
**Independent Test**: Clicking「通过」on a pending item redirects to the edit form with fields pre-filled from raw_data; clicking「批量通过」with 3 items selected shows success toast with count; banner list shows items ordered by display_order.
**Prompt**: `tasks/WP25-admin-crawler-review-and-banner.md`
**Estimated Size**: ~340 lines

### Included Subtasks
- [ ] T107 Crawler review list (`/admin/crawler`): TDesign table with columns (source, content_type, raw_data preview/title, review_status badge, created_at); status tab filter (待审核/已通过/已拒绝); batch select checkbox column; 「批量通过」button (active when ≥1 selected)
- [ ] T108 Review detail + approve flow: click row → navigate to `/admin/crawler/:id` showing raw_data formatted fields side-by-side with entity field preview; 「通过」button → call approve API → receive `{prefilled_data, content_type}` → redirect to appropriate create form (`/admin/content/{type}/new`) with form pre-populated from prefilled_data
- [ ] T109 Reject + Reset workflow: 「拒绝」button in list and detail → call reject API → update badge; on rejected items, show 「重置为待审核」button → call reset API; both update the row status in the TDesign table without full page reload
- [ ] T110 Banner management (`/admin/banner`): table with columns (content_type/id preview, display_order, start_at/end_at time range pickers, actions); 新增 button → dialog with content type selector (搜索 movie/TV/anime by title) + display_order input + time range pickers; edit inline or via dialog; delete with confirm; `t-date-range-picker` for time range

### Implementation Notes
- Raw data preview in review list: extract `raw_data.title_cn` (or `raw_data.title`) for the preview column
- Pre-fill redirect: store prefilled_data in Pinia store before redirect; new form page reads from store on mount then clears it
- Banner time range: both start_at and end_at are optional — empty means 「立即生效 / 永久有效」; show 「永久」label when both null

### Dependencies
- Depends on WP23 (admin scaffold + patterns)

---

## Phase 7: Observability & Deployment

---

## Work Package WP26: Observability + Deployment Setup (Priority: P2)

**Goal**: Add Sentry error tracking (backend + frontend), Prometheus metrics, configure Nginx, and provide Docker Compose local dev setup plus CI/CD pipeline stubs.
**Independent Test**: Triggering an unhandled exception sends event to Sentry DSN. `curl http://localhost:5001/metrics` returns Prometheus text format with HTTP request duration histograms. `docker compose up` brings all services online with health checks passing.
**Prompt**: `tasks/WP26-observability-and-deployment.md`
**Estimated Size**: ~350 lines

### Included Subtasks
- [ ] T111 Sentry integration: backend (`Sentry.AspNetCore` NuGet, `UseSentry()` in Program.cs, capture user context from JWT claims); frontend (`@sentry/vue` npm, `Sentry.init()` in main.ts for both frontend and admin apps); verify error capture in staging before production
- [ ] T112 Prometheus metrics: add `prometheus-net.AspNetCore` NuGet; expose `/metrics` endpoint (HTTP request count, duration histogram by route+status, active connections); Grafana dashboard template (JSON import) for basic HTTP monitoring; document scrape config in README
- [ ] T113 Nginx configuration: upstream `api` block (localhost:5001); `location /api/` → proxy_pass with `proxy_set_header`; `location /` → serve `/frontend/dist` static files (try_files fallback for SPA routing); `location /admin/` → serve `/admin/dist`; gzip compression; browser cache headers for assets (1 year) vs HTML (no-store)
- [ ] T114 Docker Compose (`docker-compose.yml`): services — `postgres` (postgres:15 + zhparser pre-installed image), `redis` (redis:7-alpine), `api` (.NET API image), `frontend` (Nginx serving built frontend), `admin` (Nginx serving built admin); health checks; volume mounts for persistent data; `.env` file for secrets
- [ ] T115 CI/CD pipeline stubs (GitHub Actions or equivalent): `build-and-test.yml` (4 jobs: dotnet build+test, frontend npm build, admin npm build, crawler pytest); `deploy.yml` (build images + push to registry + SSH deploy on main branch push); document required secrets in README

### Implementation Notes
- Sentry frontend: configure `tracesSampleRate: 0.1` (10% performance tracing) to control cost
- Docker image for .NET: use `mcr.microsoft.com/dotnet/aspnet:10.0` as runtime, `sdk:10.0` as build stage (multi-stage Dockerfile)
- PostgreSQL + zhparser: build custom Docker image `FROM postgres:15` + compile zhparser from source, OR use `registry.cn-hangzhou.aliyuncs.com/zhparser/zhparser-pg15` if available

### Dependencies
- Depends on WP02 (backend scaffold), WP14 (frontend scaffold) — but can be done in parallel once those are started

---

## Dependency & Execution Summary

```
WP01 (DB Schema)
  ├── WP02 (.NET Scaffold)
  │     └── WP03 (Infrastructure Services)
  │           ├── WP04 (Home + Movie List API) [P1 MVP]
  │           │     └── WP05 (Movie Detail API) [P1 MVP]
  │           ├── WP06 (TV Series API)
  │           ├── WP07 (Anime API)
  │           ├── WP08 (People + Awards API)
  │           ├── WP09 (Search + Rankings API)
  │           ├── WP10 (Admin Content CRUD)
  │           │     └── WP11 (Crawler Review + Banner)
  │           └── WP12 (Popularity + Cron Jobs)
  └── WP13 (Scrapy Crawler) [parallel, starts after WP01]

WP14 (Frontend Scaffold + Components) [no backend dependency]
  └── WP15 (NavBar + Composables)
        ├── WP16 (Home Page) [P1 MVP]
        ├── WP17 (Movie List Page) [P1 MVP]
        ├── WP18 (Movie Detail Page) [P1 MVP]
        ├── WP19 (TV List + Detail)
        ├── WP20 (Anime List + Detail)  [depends on WP19 for SeasonAccordion]
        ├── WP21 (Person + Franchise + Awards)
        └── WP22 (Search + Rankings + SEO)

WP23 (Admin Scaffold + Dashboard) [no backend dependency]
  ├── WP24 (TV/Anime/Person/Franchise Admin)
  └── WP25 (Crawler Review + Banner Admin)

WP26 (Observability) [depends on WP02 + WP14]
```

**Parallelization Highlights**:
- Backend (WP02–WP12) and Frontend (WP14–WP22) can be built simultaneously
- Admin (WP23–WP25) is fully independent from main frontend
- WP06, WP07, WP08, WP09, WP10 can all be built in parallel after WP03 is done
- WP16–WP22 can all be built in parallel after WP15 is done

**MVP Scope (Phase 1)**: WP01 → WP02 → WP03 → WP04 → WP05 → WP14 → WP15 → WP16 → WP17 → WP18

---

## Subtask Index (Reference)

| Subtask ID | Summary | Work Package | Priority | Parallel? |
|------------|---------|--------------|----------|-----------|
| T001 | Initialize monorepo directory structure | WP01 | P0 | Yes |
| T002 | Migration: movies, tv_series, anime tables | WP01 | P0 | No |
| T003 | Migration: season/episode tables | WP01 | P0 | No |
| T004 | Migration: people, credits, franchises, keywords | WP01 | P0 | No |
| T005 | Migration: media_videos, awards, banners, pending, page_views | WP01 | P0 | No |
| T006 | Configure zhparser FTS | WP01 | P0 | No |
| T007 | .NET solution structure (4 layers) | WP02 | P0 | No |
| T008 | Domain entities with SqlSugar attributes | WP02 | P0 | Yes |
| T009 | IRepository interfaces + SqlSugar base impl | WP02 | P0 | No |
| T010 | SqlSugar DI + UnitOfWork | WP02 | P0 | No |
| T011 | Application layer scaffold + base DTOs | WP02 | P0 | No |
| T012 | Redis cache service + CacheKeys | WP03 | P0 | Yes |
| T013 | COS storage client | WP03 | P0 | Yes |
| T014 | Global middleware (exception, logging, CORS, Swagger) | WP03 | P0 | No |
| T015 | OAuth 2.0 JWT RS256 auth | WP03 | P0 | No |
| T016 | Sentry + Prometheus integration | WP03 | P0 | No |
| T017 | GET /home endpoint | WP04 | P1 | Yes |
| T018 | GET /movies list endpoint with all filters | WP04 | P1 | Yes |
| T019 | Array filter SQL helper + decade range | WP04 | P1 | No |
| T020 | Redis cache invalidation strategy | WP04 | P1 | No |
| T021 | GET /movies/:id full detail DTO | WP05 | P1 | Yes |
| T022 | GET /movies/:id/credits | WP05 | P1 | Yes |
| T023 | SimilarContentService (keyword+genre overlap) | WP05 | P1 | Yes |
| T024 | GET /franchises/:id | WP05 | P1 | Yes |
| T025 | GET /tv list endpoint | WP06 | P2 | Yes |
| T026 | GET /tv/:id detail | WP06 | P2 | Yes |
| T027 | GET /tv/:id/seasons/:n | WP06 | P2 | No |
| T028 | GET /tv/:id/similar | WP06 | P2 | Yes |
| T029 | GET /anime list endpoint | WP07 | P2 | Yes |
| T030 | GET /anime/:id detail | WP07 | P2 | Yes |
| T031 | GET /anime/:id/seasons/:n | WP07 | P2 | No |
| T032 | GET /anime/:id/similar | WP07 | P2 | Yes |
| T033 | GET /people/:id PersonDetail | WP08 | P2 | Yes |
| T034 | Collaborator top-8 query | WP08 | P2 | No |
| T035 | GET /awards/:slug | WP08 | P2 | Yes |
| T036 | GET /awards/:slug/:edition | WP08 | P2 | Yes |
| T037 | GET /search full-text + fallback | WP09 | P2 | No |
| T038 | GET /search/autocomplete | WP09 | P2 | No |
| T039 | GET /rankings hot+score+Top100 | WP09 | P2 | No |
| T040 | Movie admin CRUD | WP10 | P3 | Yes |
| T041 | TV Series admin CRUD + season/episode sub-resources | WP10 | P3 | Yes |
| T042 | Anime admin CRUD | WP10 | P3 | Yes |
| T043 | Person + Franchise admin CRUD | WP10 | P3 | Yes |
| T044 | GET /admin/stats + keyword search | WP10 | P3 | Yes |
| T045 | GET /admin/pending list + detail | WP11 | P3 | No |
| T046 | POST /admin/pending/:id/approve + pre-fill | WP11 | P3 | No |
| T047 | POST /admin/pending/:id/reject + /reset | WP11 | P3 | No |
| T048 | POST /admin/pending/bulk-approve | WP11 | P3 | No |
| T049 | Banner CRUD endpoints | WP11 | P3 | Yes |
| T050 | POST /tracking/view + page_views insert | WP12 | P2 | No |
| T051 | Daily popularity update background job | WP12 | P2 | No |
| T052 | Daily rankings cache refresh job | WP12 | P2 | No |
| T053 | Scrapy project setup | WP13 | P3 | No |
| T054 | Proxy + UA middleware | WP13 | P3 | Yes |
| T055 | Dedup pipeline | WP13 | P3 | Yes |
| T056 | PostgreSQL write pipeline | WP13 | P3 | No |
| T057 | TMDB API spider | WP13 | P3 | Yes |
| T058 | Douban HTML spider | WP13 | P3 | Yes |
| T059 | Mtime HTML spider | WP13 | P3 | Yes |
| T060 | Vue 3 frontend project setup | WP14 | P1 | No |
| T061 | MediaCard component | WP14 | P1 | Yes |
| T062 | Pagination component | WP14 | P1 | Yes |
| T063 | FilterBar + DropdownFilter components | WP14 | P1 | Yes |
| T064 | Lightbox component | WP14 | P1 | Yes |
| T065 | ImageTabBlock component | WP14 | P1 | Yes |
| T066 | NavBar component | WP15 | P1 | Yes |
| T067 | Footer component | WP15 | P1 | Yes |
| T068 | SearchBar with autocomplete | WP15 | P1 | No |
| T069 | useFilters composable | WP15 | P1 | No |
| T070 | useSearch + usePagination composables | WP15 | P1 | No |
| T071 | Hero Banner carousel | WP16 | P1 | No |
| T072 | Hot lists horizontal scroll sections | WP16 | P1 | Yes |
| T073 | Hot anime with 国漫/日漫 tabs | WP16 | P1 | Yes |
| T074 | Rankings + Awards entry cards | WP16 | P1 | Yes |
| T075 | Movie list FilterBar (genre/region/decade rows) | WP17 | P1 | No |
| T076 | Language/score dropdowns + sort controls + grid | WP17 | P1 | No |
| T077 | URL ↔ filter bidirectional sync | WP17 | P1 | No |
| T078 | Pagination + loading skeleton + empty state | WP17 | P1 | No |
| T079 | Movie detail Hero section + `<title>` + meta | WP18 | P1 | No |
| T080 | Ratings block (Douban dist bars + IMDB + Mtime) | WP18 | P1 | Yes |
| T081 | Cast grid + Synopsis collapse + Videos tabs | WP18 | P1 | Yes |
| T082 | Awards + Franchise + Similar blocks | WP18 | P1 | Yes |
| T083 | ImageTabBlock placement + responsive layout | WP18 | P1 | No |
| T084 | TV list page with air_status filter | WP19 | P2 | No |
| T085 | TV detail Hero + Next Episode block | WP19 | P2 | No |
| T086 | SeasonAccordion component | WP19 | P2 | No |
| T087 | TV season detail page + prev/next nav | WP19 | P2 | No |
| T088 | Anime list with origin tabs + source_material filter | WP20 | P2 | No |
| T089 | Anime detail page with studio/voice actor blocks | WP20 | P2 | No |
| T090 | Anime season detail page | WP20 | P2 | No |
| T091 | Person detail page (profile + works tabs) | WP21 | P2 | No |
| T092 | CollaboratorBlock + PhotoWall components | WP21 | P2 | Yes |
| T093 | Franchise detail page | WP21 | P2 | Yes |
| T094 | Awards main + edition detail pages | WP21 | P2 | Yes |
| T095 | Search results page | WP22 | P2 | Yes |
| T096 | Rankings page | WP22 | P2 | Yes |
| T097 | SEO meta composable + page titles | WP22 | P2 | No |
| T098 | Image lazy loading + critical preload | WP22 | P2 | Yes |
| T099 | Admin project setup + OAuth PKCE login | WP23 | P3 | No |
| T100 | Admin layout + Dashboard stats page | WP23 | P3 | No |
| T101 | Movie admin list page | WP23 | P3 | No |
| T102 | Movie admin create/edit form | WP23 | P3 | No |
| T103 | TV Series admin CRUD pages | WP24 | P3 | Yes |
| T104 | Anime admin CRUD pages | WP24 | P3 | Yes |
| T105 | Person admin CRUD pages | WP24 | P3 | Yes |
| T106 | Franchise admin CRUD pages | WP24 | P3 | Yes |
| T107 | Crawler review list | WP25 | P3 | No |
| T108 | Review detail + approve flow | WP25 | P3 | No |
| T109 | Reject + Reset workflow | WP25 | P3 | No |
| T110 | Banner management page | WP25 | P3 | Yes |
| T111 | Sentry integration (backend + frontend) | WP26 | P2 | Yes |
| T112 | Prometheus metrics endpoint | WP26 | P2 | Yes |
| T113 | Nginx configuration | WP26 | P2 | No |
| T114 | Docker Compose local dev setup | WP26 | P2 | No |
| T115 | CI/CD pipeline stubs | WP26 | P2 | No |

---

> This tasks.md was generated by `/spec-kitty.tasks`. Each WP prompt file contains detailed implementation guidance. 26 work packages, 115 subtasks total.
