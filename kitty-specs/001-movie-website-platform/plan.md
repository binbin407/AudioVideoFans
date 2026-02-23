# Implementation Plan: 影视资讯网站平台

**Branch**: `master` | **Date**: 2026-02-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `kitty-specs/001-movie-website-platform/spec.md`

---

## Summary

构建一个面向中国大陆用户的纯内容展示类影视资讯网站，涵盖电影、电视剧、动漫三大品类及影人数据库。系统由三个独立前端子系统（展示前端 Vue 3 + Vite SPA、管理后台 Vue 3 + TDesign Vue）和一个 RESTful API 后端（.NET Core 10 + DDD 四层架构 + SqlSugar）组成，通过 Python Scrapy 爬虫采集数据至 `pending_content` 暂存区，经管理员审核后对外展示。图片存储于腾讯云 COS，搜索基于 PostgreSQL 15 zhparser 全文索引，缓存层使用 Redis，整体以 Monorepo 组织。

---

## Technical Context

**Language/Version**: C# (.NET 10)、TypeScript（strict: true）、Python 3.11+
**Primary Dependencies**: ASP.NET Core 10 Web API、SqlSugar、Vue 3 + Vite + Tailwind CSS、TDesign Vue、Python Scrapy 2.x
**Storage**: PostgreSQL 15+（主数据库）、Redis（缓存层，详情页 TTL 1h / 列表 10min / 排行榜 24h）、腾讯云 COS + CDN
**Testing**: xUnit（后端单元/集成测试，覆盖率 ≥ 80%，核心路径 100%）、Vitest（前端关键组件）、pytest（爬虫管道）
**Target Platform**: 腾讯云 CVM（.NET Core API）+ CDB for PostgreSQL + TencentDB for Redis + COS + CDN；前端通过 Nginx 静态托管
**Project Type**: Web Monorepo（4 子系统：`/frontend` / `/admin` / `/api` / `/crawler`）
**Performance Goals**: 首页 LCP ≤ 2.5s（移动端 4G）、搜索 P95 ≤ 500ms、列表筛选 ≤ 300ms
**Constraints**: 峰值 > 5000 并发用户、SLA 99.9%（每月计划外停机 ≤ 44 分钟）
**Scale/Scope**: Phase 1 上线 ≥ 500 部电影；15+ 实体类型；48 条功能需求

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 条目 | 宪法要求 | 当前设计 | 状态 |
|------|---------|---------|------|
| 展示前端框架 | Vue 3 + TypeScript + Vite + Tailwind CSS | Vue 3 + Vite SPA ✓ | **PASS** |
| 管理前端框架 | Vue 3 + TypeScript + Vite + TDesign Vue | Vue 3 + TDesign Vue ✓ | **PASS** |
| 后端框架 | .NET Core 10 Web API + DDD 四层 | .NET Core 10 + DDD ✓ | **PASS** |
| ORM | SqlSugar | SqlSugar ✓ | **PASS** |
| SEO 目标（Lighthouse ≥ 75，SPA 架构） | 宪法「经验教训」建议 Nuxt 3 SSR | Vue 3 Vite SPA，采用量化 SEO 门槛（Lighthouse SEO ≥ 75）并通过 metadata/sitemap/prerender 优化 | **ACCEPTED TRADEOFF** |
| 后端测试覆盖率 | ≥ 80%，核心路径 100% | xUnit，任务拆分时强制覆盖率门禁 | **PASS** |
| 软删除 | `deleted_at` 字段，禁止物理删除 | 所有可发布实体含 `deleted_at` | **PASS** |
| 缓存主动失效 | 编辑后清除对应 Redis key | Application Layer 负责清除，不依赖 TTL | **PASS** |
| 图片存储解耦 | 数据库存 COS key，不存完整 URL | 所有图片字段均为 `_cos_key` 后缀 | **PASS** |
| 爬虫暂存区 | pending_content → 审核 → 发布 | FR-22 定义完整流程 | **PASS** |
| DDD 分层禁止跨层 | API 层不得直接访问 Repository | Controller → Application Service → Repository 接口 | **PASS** |

**Constitution Check Result**: ✅ 1 项已接受权衡（SEO 目标降级），其余全部通过。可进入 Phase 0。

---

## Project Structure

### Documentation (this feature)

```
kitty-specs/001-movie-website-platform/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── README.md
│   ├── movies.yaml
│   ├── tv.yaml
│   ├── anime.yaml
│   ├── people.yaml
│   ├── search.yaml
│   ├── rankings.yaml
│   ├── awards.yaml
│   └── admin.yaml
└── tasks.md             # Phase 2 output (NOT created by /spec-kitty.plan)
```

### Source Code (Monorepo)

```
/
├── frontend/                        # 展示前端 (Vue 3 + Vite + Tailwind CSS)
│   ├── src/
│   │   ├── assets/
│   │   ├── components/
│   │   │   ├── common/              # MediaCard, Pagination, Lightbox, FilterBar, RatingBar
│   │   │   ├── movie/               # MovieDetailHero, FranchiseBlock, AwardBlock
│   │   │   ├── tv/                  # TvDetailHero, SeasonAccordion, NextEpisodeBlock
│   │   │   ├── anime/               # AnimeDetailHero, StudioBlock
│   │   │   ├── person/              # PersonProfile, CollaboratorBlock, PhotoWall
│   │   │   └── layout/              # NavBar, Footer, SearchBar
│   │   ├── pages/
│   │   │   ├── index.vue            # 首页 /
│   │   │   ├── movies/
│   │   │   │   ├── index.vue        # /movies 列表
│   │   │   │   └── [id].vue         # /movies/[id] 详情
│   │   │   ├── tv/
│   │   │   │   ├── index.vue        # /tv 列表
│   │   │   │   ├── [id].vue         # /tv/[id] 详情
│   │   │   │   └── [id]/season/[n].vue  # /tv/[id]/season/[n] 季详情
│   │   │   ├── anime/
│   │   │   │   ├── index.vue        # /anime 列表
│   │   │   │   ├── [id].vue         # /anime/[id] 详情
│   │   │   │   └── [id]/season/[n].vue  # /anime/[id]/season/[n] 季详情
│   │   │   ├── people/[id].vue      # /people/[id] 影人详情
│   │   │   ├── franchises/[id].vue  # /franchises/[id] 系列详情
│   │   │   ├── awards/
│   │   │   │   ├── [slug].vue       # /awards/[slug] 奖项主页
│   │   │   │   └── [slug]/[edition].vue  # /awards/[slug]/[edition] 届次
│   │   │   ├── rankings/index.vue   # /rankings 排行榜
│   │   │   └── search/index.vue     # /search 搜索结果
│   │   ├── composables/
│   │   │   ├── useFilters.ts        # 筛选状态与 URL 同步
│   │   │   ├── usePagination.ts
│   │   │   ├── useLightbox.ts
│   │   │   └── useSearch.ts
│   │   ├── stores/                  # Pinia stores
│   │   └── api/                     # Axios API 客户端
│   ├── tests/
│   │   └── components/              # Vitest 组件测试
│   └── vite.config.ts
│
├── admin/                           # 管理后台 (Vue 3 + Vite + TDesign Vue)
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   │   ├── dashboard/           # 概览统计
│   │   │   ├── content/
│   │   │   │   ├── movies/          # 电影增删改查
│   │   │   │   ├── tv/              # 电视剧增删改查
│   │   │   │   ├── anime/           # 动漫增删改查
│   │   │   │   ├── people/          # 影人增删改查
│   │   │   │   └── franchises/      # 系列管理
│   │   │   ├── crawler/             # 爬虫审核（pending_content）
│   │   │   ├── banner/              # Hero Banner 配置
│   │   │   └── awards/              # 奖项管理
│   │   └── api/
│   └── tests/
│
├── api/                             # 后端 API (.NET Core 10 DDD)
│   ├── src/
│   │   ├── Domain/
│   │   │   ├── Entities/            # Movie, TvSeries, Anime, Person, Season, Episode...
│   │   │   ├── Repositories/        # IRepository<T>, IMovieRepository...
│   │   │   └── Services/            # SimilarContentService, PopularityService
│   │   ├── Application/
│   │   │   ├── Movies/              # GetMovieListQuery, GetMovieDetailQuery, CreateMovieCommand...
│   │   │   ├── TvSeries/
│   │   │   ├── Anime/
│   │   │   ├── People/
│   │   │   ├── Search/              # SearchQuery, AutocompleteQuery
│   │   │   ├── Rankings/            # GetRankingsQuery
│   │   │   ├── Awards/
│   │   │   └── Admin/               # ApproveContentCommand, BulkApproveCommand
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/         # SqlSugar repositories, DbContext
│   │   │   ├── Cache/               # RedisCache, CacheKeys
│   │   │   └── Storage/             # TencentCosClient
│   │   └── API/
│   │       ├── Controllers/
│   │       └── Middleware/          # Auth, GlobalException, RequestLogging
│   └── tests/
│       ├── Unit/                    # Domain + Application layer tests
│       └── Integration/             # Repository + API endpoint tests
│
└── crawler/                         # 爬虫 (Python 3.11 + Scrapy)
    ├── spiders/
    │   ├── tmdb_spider.py           # TMDB API (官方 API)
    │   ├── douban_spider.py         # 豆瓣 HTML 解析
    │   └── mtime_spider.py          # 时光网评分
    ├── pipelines/
    │   ├── dedup_pipeline.py        # 基于 source_url 去重
    │   └── postgres_pipeline.py     # 写入 pending_content
    ├── middlewares/
    │   ├── proxy_middleware.py      # HTTP 代理池轮换
    │   └── useragent_middleware.py  # 随机 UA 池
    ├── settings.py                  # 配置：DOWNLOAD_DELAY ≥ 3s
    └── tests/
```

**Structure Decision**: Monorepo 单仓库，4 子系统独立目录，无共享包（避免跨语言耦合）。统一 CI/CD 流水线。

---

## Phase 0 Artifacts

→ 见 [research.md](./research.md)

关键研究决策点：
- SqlSugar + PostgreSQL DDD 仓储模式（含 JSONB / TEXT[] 处理）
- PostgreSQL 15 zhparser 中文全文搜索配置与 tsvector 维护策略
- Python Scrapy 代理池与 UA 轮换反爬最佳实践
- Vue 3 Vite SPA SEO 优化（meta 标签、prerender、sitemap）

---

## Phase 1 Artifacts

→ 见 [data-model.md](./data-model.md) | [contracts/README.md](./contracts/README.md) | [quickstart.md](./quickstart.md)

---

## Complexity Tracking

| 决策 | 理由 | 更简方案被拒原因 |
|------|------|----------------|
| `tv_series` / `anime` 完全独立建表 | 用户明确选择（planning Q2） | 单表方案 NULL 字段过多，查询频繁判断 type |
| Vue 3 Vite SPA（放弃 SSR） | 用户明确选择（planning Q1），接受 SEO 降级 | Nuxt 3 增加架构复杂度，团队倾向 SPA |
| DDD 四层架构 | 宪法强制要求 | 简单 CRUD 层在 48 个 FR 规模下难以维护 |
| 多态关联（Credit/Keyword/Video 用 content_type + content_id） | 简化关联表设计，避免为每类内容建独立关联表 | 3 套独立关联表方案导致 Application Layer 大量重复代码 |
