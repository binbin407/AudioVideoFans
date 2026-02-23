# 工作包：影视资讯网站平台

**输入文档**: `kitty-specs/001-movie-website-platform/` 中的设计文档
**前置条件**: plan.md ✓, spec.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**测试说明**: WP27（后端 xUnit，覆盖率 ≥80%）、WP28（前端 Vitest，关键组件）和 WP29（爬虫 pytest，管道 + 蜘蛛测试）按章程要求添加。子任务 T116–T130 涵盖测试配置、领域单元测试、应用服务测试、仓储集成测试、组件测试和爬虫管道测试。

**组织结构**: 147 个细粒度子任务（T001–T147），分组为 29 个工作包（WP01–WP29）。每个工作包可在一次专注会话中独立交付。

---

## 第0阶段：基础设施

---

## 工作包 WP01：Monorepo 搭建与数据库 Schema（优先级：P0）🎯 基础

**目标**: 初始化 4 子系统 monorepo 结构，创建所有 PostgreSQL 数据库表、索引及全文检索配置。
**独立验收测试**: 所有迁移在安装了 zhparser 的全新 PostgreSQL 15 实例上可干净应用。`\dt` 列出所有 18 张表，且列数正确。
**提示文件**: `tasks/WP01-monorepo-setup-and-db-schema.md`
**预估规模**: 约 380 行

### 包含子任务
- [x] T001 初始化 monorepo 目录结构（`/frontend`、`/admin`、`/api`、`/crawler`，根目录 CI 配置存根）
- [x] T002 编写 PostgreSQL 迁移：核心内容表（`movies`、`tv_series`、`anime`，包含所有列、约束、索引和 FTS 生成列）
- [x] T003 编写 PostgreSQL 迁移：季/集表（`tv_seasons`、`tv_episodes`、`anime_seasons`、`anime_episodes`）
- [x] T004 编写 PostgreSQL 迁移：people、credits、franchises、keywords、content_keywords
- [x] T005 编写 PostgreSQL 迁移：media_videos、award_events/ceremonies/nominations、featured_banners、pending_content、page_views
- [x] T006 配置 zhparser FTS：安装扩展，创建 `chinese_zh` TEXT SEARCH CONFIGURATION，验证 `search_vector` 生成列正常工作

### 实施说明
- 在 `api/` 中使用 SqlSugar 进行实体映射与迁移脚本管理；复杂索引、FTS 配置与扩展安装通过原始 SQL 迁移脚本执行
- 禁止在本项目引入 EF Core 作为迁移或 ORM 主路径，以保持与宪法一致
- 所有实体表包含 `deleted_at TIMESTAMPTZ` 字段用于软删除
- `page_views` 表（content_type, content_id, viewed_at）是热度评分所必需的（WP12）
- `search_vector` 是 STORED 生成列——无需触发器

### 并行机会
- T001（monorepo 脚手架）可在编写 T002–T006 SQL 的同时进行

### 依赖关系
- 无（起始工作包）

### 风险与对策
- zhparser 在某些 PostgreSQL Docker 镜像上可能安装失败 → 使用预置扩展的 `pgsql/postgresql:15`，并记录备用方案
- 生成列语法在不同 PostgreSQL 版本间略有差异 → 在 15.x 上专项测试

---

## 工作包 WP02：.NET Core 10 DDD 后端脚手架（优先级：P0）

**目标**: 创建具有 DDD 四层架构的 .NET Core 10 解决方案，包括所有领域实体、仓储接口及基础 SqlSugar 基础设施。
**独立验收测试**: `dotnet build` 成功；空 API 启动于 `https://localhost:5001`；DI 解析所有注册服务无报错。
**提示文件**: `tasks/WP02-dotnet-ddd-scaffold.md`
**预估规模**: 约 360 行

### 包含子任务
- [x] T007 创建 .NET 解决方案结构：Domain / Application / Infrastructure / API 项目及项目引用关系
- [x] T008 定义所有带 SqlSugar 特性的领域实体：Movie、TvSeries、Anime、TvSeason、TvEpisode、AnimeSeason、AnimeEpisode、Person、Credit、Franchise、Keyword、ContentKeyword、MediaVideo、AwardEvent、AwardCeremony、AwardNomination、FeaturedBanner、PendingContent
- [x] T009 在 Domain 中定义 IRepository\<T\> 泛型接口及专用接口（IMovieRepository、ITvSeriesRepository 等）；在 Infrastructure 中实现 SqlSugarRepository\<T\> 基类
- [x] T010 配置 SqlSugar DI（Scoped ISqlSugarClient，PgSqlIsAutoToLower = false），实现 IUnitOfWork + UnitOfWork
- [x] T011 搭建 Application 层：命令/查询处理器结构（不需要 MediatR；使用直接服务类），所有内容类型的基础 DTO

### 实施说明
- `PgSqlIsAutoToLower = false` 至关重要——若缺少此配置，SqlSugar 将把列名映射为小写，导致 PostgreSQL 列解析失败
- TEXT[] 列：使用 `[SugarColumn(ColumnDataType = "text[]")]`；JSONB：使用 `IsJson = true`
- 生成列（`search_vector`）：标记为 `IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true`
- PostgreSQL 数组过滤器（`&&` 运算符）必须使用原始 SQL——SqlSugar 无法转换这类查询

### 并行机会
- T007（解决方案结构）可立即启动；若有多名开发者，T008 实体定义可按开发者分工并行完成

### 依赖关系
- 依赖 WP01（DB Schema 定义列结构）

### 风险与对策
- SqlSugar JSONB 映射需要精确的 `ColumnDataType`——对每个 JSONB 列测试序列化/反序列化的完整往返

---

## 工作包 WP03：后端基础设施服务（优先级：P0）

**目标**: 实现 Redis 缓存层、腾讯 COS 存储客户端、全局中间件（异常处理、请求日志、CORS、Swagger）及 OAuth 2.0 JWT 鉴权。
**独立验收测试**: `GET /api/v1/health` 返回 200；`GET /api/admin/stats` 未携带 token 时返回 401；Swagger UI 在 `/swagger` 加载成功。
**提示文件**: `tasks/WP03-backend-infrastructure-services.md`
**预估规模**: 约 320 行

### 包含子任务
- [ ] T012 Redis 缓存服务：实现 `IRedisCache`（Get/Set/Delete/Exists），定义 `CacheKeys` 常量类，键名规范匹配（例如 `movie:detail:{id}`、`movies:list:{hash}`）
- [ ] T013 腾讯 COS 存储客户端：`ITencentCosClient` 接口 + 上传/删除实现；`CosUrlHelper.GetCdnUrl(cosKey)` 工具方法
- [ ] T014 全局中间件：`GlobalExceptionMiddleware`（统一捕获 → 结构化错误 JSON）、`RequestLoggingMiddleware`（方法、路径、状态码、耗时）、CORS 策略、带 JWT bearer 的 Swagger/OpenAPI
- [ ] T015 OAuth 2.0 JWT RS256 鉴权：配置 `AddAuthentication().AddJwtBearer()`，携带 JWKS URI；对所有 `/api/admin/**` 控制器使用 `[Authorize]`
- [ ] T016 集成 Sentry SDK：在 Program.cs 中调用 `UseSentry()`；通过 `prometheus-net.AspNetCore` 暴露 Prometheus /metrics 端点；在开发模式下验证错误捕获

### 实施说明
- 列表查询的 Redis 键哈希：`MD5(JsonSerializer.Serialize(filterDto))` → 生成简短且确定性的缓存键
- 缓存失效：Application Layer 服务在任何保存/更新操作后必须调用 `_redis.Delete(CacheKeys.MovieDetail(id))`
- COS 客户端使用 `COSSTS.NET` SDK 或直接 HTTP——数据库中仅存储 COS 对象键，永不存储完整 URL

### 并行机会
- T012（Redis）和 T013（COS）相互独立，可并行完成

### 依赖关系
- 依赖 WP02（解决方案结构必须存在）

### 风险与对策
- 开发环境中 Sentry DSN 必须为空字符串（而非 null）以避免初始化错误 → 使用 `if (!string.IsNullOrEmpty(dsn))` 守卫

---

## 第1阶段：核心后端 API（电影 + 首页）

---

## 工作包 WP04：首页 + 电影列表 API（优先级：P1）🎯 MVP

**目标**: 实现首页聚合端点和电影列表端点，支持所有筛选/排序/分页维度。
**独立验收测试**: `GET /api/v1/home` 返回 Banner + 各热门列表各含 ≥1 条数据（需种子数据）。`GET /api/v1/movies?genre=sci-fi&region=us&decade=2020s&sort=douban_score&page=1` 返回过滤结果，且包含正确的 `pagination` 对象。
**提示文件**: `tasks/WP04-home-and-movie-list-api.md`
**预估规模**: 约 310 行

### 包含子任务
- [ ] T017 `GET /api/v1/home`：HomeController + HomeApplicationService——获取有效 FeaturedBanners（start_at/end_at 时间过滤）、热门电影（热度 top 8）、热门电视剧（top 8）、按产地的热门动漫（国漫 top 8，日漫 top 8）；Redis 缓存 `home:banners` 10 分钟
- [ ] T018 `GET /api/v1/movies`：MovieListQuery，含过滤参数（genre、region、decade、year、lang、评分阈值、sort、page、page_size）；使用原始 SQL 的 PostgreSQL 数组 `&&` 过滤器用于 genres/region/language；Redis 缓存 `movies:list:{hash}` 10 分钟
- [ ] T019 数组过滤 SQL 辅助工具：`ArrayFilterHelper.BuildWhereClause(filters)` 为 TEXT[] 重叠过滤器生成参数化 SQL 片段；年代到年份范围的转换（2020s → 2020–2029）
- [ ] T020 缓存失效策略：在任何 Movie 创建/更新后，Application Layer 调用 `_redis.DeletePattern("movies:list:*")` 和 `_redis.Delete(CacheKeys.MovieDetail(id))`

### 实施说明
- 首页端点必须遵守 Banner 的 `start_at ≤ NOW() ≤ end_at`（或 NULL 边界）
- 当 `douban_score IS NULL` 时，从评分过滤结果中排除（不视为 0）
- 排序选项：`douban_score DESC NULLS LAST`、`popularity DESC`、`release_date DESC NULLS LAST`（使用 `release_dates` JSONB 中最早的国内上映日期）
- 每个列表响应使用 `MediaCardDto`（id、title_cn、year、poster_cos_key、douban_score、genres）

### 并行机会
- T017（首页）和 T018（电影列表）可并行构建

### 依赖关系
- 依赖 WP02（实体）、WP03（Redis、中间件）

### 风险与对策
- `release_dates` 年份提取的 JSONB 数组过滤需要 PostgreSQL JSONB 运算符——在原始 SQL 中使用 `jsonb_array_elements(release_dates)->>'date'`
- Redis 的 `DeletePattern` 需要 SCAN 命令——实现时务必小心，避免阻塞生产环境

---

## 工作包 WP05：电影详情 API（优先级：P1）🎯 MVP

**目标**: 实现完整的电影详情端点，包括演职员表、相似内容和系列详情页 API。
**独立验收测试**: `GET /api/v1/movies/1` 返回所有区块（franchise 块对 null 安全，awards 为空数组可接受）。`GET /api/v1/movies/1/similar` 返回 ≤6 条按关键词重叠度排序的结果。
**提示文件**: `tasks/WP05-movie-detail-api.md`
**预估规模**: 约 300 行

### 包含子任务
- [ ] T021 `GET /api/v1/movies/:id`：组装 MovieDetailDto——基础字段 + franchise（含序号/总数）、主演（按 display_order 前 20）、导演、奖项（该电影在各届颁奖典礼的所有提名）、视频（按类型）、相似内容（6 条）、extra_posters/backdrops；Redis 缓存 `movie:detail:{id}` 1 小时
- [ ] T022 `GET /api/v1/movies/:id/credits`：按部门分组的完整分页演职员表（导演/编剧/主演/制片人/其他）；不使用 Redis 缓存（访问频率低）
- [ ] T023 `SimilarContentService`：给定（content_type、content_id），查找最多 6 条相似内容——排序优先级固定为：关键词重叠数量 DESC → genre 重叠度 DESC → publish_time DESC → content_id ASC（最终稳定 tie-breaker）；排除自身；遵守软删除和 status=published
- [ ] T024 `GET /api/v1/franchises/:id`：FranchiseDetailDto——系列信息 + 该 franchise_id 的所有电影，按 franchise_order ASC 排序，包含海报/标题/年份/douban_score

### 实施说明
- 当 `franchise_id IS NULL` 时，`MovieDetailDto.franchise` 为 null——不包含空对象
- 奖项：JOIN award_nominations → award_ceremonies → award_events；包含 `is_winner`、`category`、`event_cn`、`edition`
- Franchise 总数：`SELECT COUNT(*) FROM movies WHERE franchise_id = :id AND deleted_at IS NULL`
- SimilarContentService 必须处理所有 3 种内容类型（movie/tv_series/anime）

### 并行机会
- T021–T024 均可并行构建（独立的控制器/服务）

### 依赖关系
- 依赖 WP04（建立 API 模式 + ArrayFilterHelper）

### 风险与对策
- 带 LEFT JOIN + GROUP BY 的关键词重叠查询在大数据集上可能变慢 → 添加 EXPLAIN ANALYZE，确保使用 `idx_content_keywords_content` 索引

---

## 工作包 WP06：电视剧 API（优先级：P2）

**目标**: 实现电视剧列表、详情、季详情端点，包含所有电视剧特有过滤器（air_status）及季/集数据。
**独立验收测试**: `GET /api/v1/tv?status=airing&genre=mystery` 仅返回播出中的剧集。`GET /api/v1/tv/456/seasons/3` 返回季头信息 + 10 集，格式为 S03E01。
**提示文件**: `tasks/WP06-tv-series-api.md`
**预估规模**: 约 300 行

### 包含子任务
- [ ] T025 `GET /api/v1/tv`：TvSeriesListQuery——与电影相同的过滤维度，另加 `air_status` 多值过滤器（airing/ended/production/cancelled）；支持按 `first_air_date DESC` 排序；Redis 缓存 `tv:list:{hash}` 10 分钟
- [ ] T026 `GET /api/v1/tv/:id`：TvSeriesDetailDto——基础信息 + `air_status` 标签 + `next_episode_info`（仅在播出中且非 null 时）+ 季摘要（每季：海报、名称、集数、首播日期、平均评分、简介）；Redis 缓存 `tv:detail:{id}` 1 小时
- [ ] T027 `GET /api/v1/tv/:id/seasons/:season_number`：SeasonDetailDto——季头字段 + 按 episode_number ASC 排序的完整集列表（每集：id、episode_number、名称、播出日期、时长、still_cos_key、简介）；包含上一季/下一季编号
- [ ] T028 `GET /api/v1/tv/:id/similar`：复用 SimilarContentService（content_type='tv_series'）
- [ ] T131 `GET /api/v1/tv/:id/credits`：返回按部门分组的完整演职员列表（导演/编剧/主演/制片人/其他），支持分页

### 实施说明
- `air_status` 过滤器支持多值（`?status=airing&status=ended`）；构建 `WHERE air_status = ANY(@statuses)` SQL
- 季详情页需要上一季/下一季：查询该剧的 MIN 和 MAX season_number，返回相邻值
- `next_episode_info` JSON 结构：`{air_date, title, season_number, episode_number}`——直接从 DB 传递

### 并行机会
- T025（列表）和 T026–T028（详情）相互独立

### 依赖关系
- 依赖 WP02、WP03（遵循 WP04/WP05 的模式）

---

## 工作包 WP07：动漫 API（优先级：P2）

**目标**: 实现动漫列表、详情和季详情端点，包含动漫特有过滤器（origin、source_material）。
**独立验收测试**: `GET /api/v1/anime?origin=cn&source=manga` 仅返回中国漫画改编动漫。`GET /api/v1/anime/789` 包含 studio、source_material 和 origin 字段。
**提示文件**: `tasks/WP07-anime-api.md`
**预估规模**: 约 290 行

### 包含子任务
- [ ] T029 `GET /api/v1/anime`：AnimeListQuery——所有标准过滤器 + `origin`（cn/jp/other）+ `source_material`（original/manga/novel/game）；Redis 缓存 `anime:list:{hash}` 10 分钟
- [ ] T030 `GET /api/v1/anime/:id`：AnimeDetailDto——所有基础字段 + `origin`、`studio`、`source_material` 字段；季摘要中配音演员演职员与其他演职员分开；Redis 缓存 `anime:detail:{id}` 1 小时
- [ ] T031 `GET /api/v1/anime/:id/seasons/:season_number`：AnimeSeason 详情，包含完整集列表及上一季/下一季导航（结构与 TV 季相同）
- [ ] T032 `GET /api/v1/anime/:id/similar`：复用 SimilarContentService（content_type='anime'）
- [ ] T132 `GET /api/v1/anime/:id/credits`：返回按部门分组的完整演职员列表，配音演员保留 `character_name`

### 实施说明
- 配音演员（role='voice_actor'）必须单独返回，演职员中包含 `character_name` 字段
- `AnimeDetailDto` 添加 `origin_label` 计算字段：cn→「国漫」，jp→「日漫」，other→「其他」
- 产地过滤：`WHERE origin = @origin`（简单等值，非数组重叠）

### 并行机会
- 可与 WP06 完全并行构建（相同模式，不同实体）

### 依赖关系
- 依赖 WP02、WP03

---

## 工作包 WP08：影人 + 奖项 API（优先级：P2）

**目标**: 实现影人详情端点（包含 top-8 合作者查询）以及奖项主页 + 届次详情端点。
**独立验收测试**: `GET /api/v1/people/888` 包含合作次数正确的 top-8 合作者。`GET /api/v1/awards/oscar/96` 返回按类别分组且含 is_winner 标记的提名。
**提示文件**: `tasks/WP08-people-and-awards-api.md`
**预估规模**: 约 310 行

### 包含子任务
- [ ] T033 `GET /api/v1/people/:id`：PersonDetailDto——个人资料字段（name_cn/en、职业、出生/死亡/籍贯/国籍/身高）、传记、作品列表（credits 中所有内容类型，按角色分组）、获奖记录（按颁奖年份 DESC，含奖项名、届次、类别、is_winner）、photos_cos_keys；Redis 缓存 `person:detail:{id}` 1 小时
- [ ] T034 合作者查询：给定 person_id，查找 top-8 合作者——JOIN credits 中拥有相同 content_type+content_id 的另一人，GROUP BY co-person_id，ORDER BY co_count DESC LIMIT 8；包含合作者头像/姓名
- [ ] T035 `GET /api/v1/awards/:slug`：AwardEventDetailDto——奖项信息（name_cn/en、描述、官方 URL）+ 所有届次列表（edition_number、年份、颁奖日期）
- [ ] T036 `GET /api/v1/awards/:slug/:edition`：CeremonyDetailDto——典礼信息 + 按类别分组的提名；每条提名包含内容信息（海报、标题）+ 影人信息 + is_winner 标记
- [ ] T137 奖项基础数据初始化：导入至少 7 个奖项事件（奥斯卡、金球奖、戛纳、威尼斯、柏林、金像奖、金马奖），生成 `award_events` 基础记录与 slug
- [ ] T138 奖项最小可展示数据集：为每个奖项至少准备 1 届 ceremony 和可渲染 nomination 数据（含 is_winner、类别、关联内容/影人），用于 `/awards/[slug]` 与 `/awards/[slug]/[edition]` 验收

### 实施说明
- 作品列表 Tab 过滤：查询参数 `role=actor|director|writer|all`——添加到 `/people/:id?role=actor` 或返回所有角色供前端过滤
- 合作者查询性能：`credits` 表在 `(content_type, content_id)` 和 `person_id` 上建有索引——使用这些索引
- 奖项提名：根据 `content_type` JOIN 到 movies/tv_series/anime 获取内容信息

### 并行机会
- T033+T034（影人）与 T035+T036（奖项）完全并行

### 依赖关系
- 依赖 WP02、WP03

---

## 工作包 WP09：搜索 + 排行榜 API（优先级：P2）

**目标**: 实现基于 zhparser 的全文搜索（含 ILIKE 回退）、自动补全，以及所有 3 种内容类型的热门/高分排行榜。
**独立验收测试**: `GET /api/v1/search?q=星际` 返回跨所有 4 种类型的结果及各类型计数。`GET /api/v1/search/autocomplete?q=星` 在 ≤100ms 内返回分组结果。`GET /api/v1/rankings` 返回所有 3 个内容类型 Tab 的数据。
**提示文件**: `tasks/WP09-search-and-rankings-api.md`
**预估规模**: 约 270 行

### 包含子任务
- [ ] T037 `GET /api/v1/search?q=`：SearchQuery——在所有 4 张表上尝试 `search_vector @@ plainto_tsquery('chinese_zh', @q)`；仅在短词或分词不可用时回退到 `title_cn ILIKE @qPrefix`（参数化前缀匹配，`@qPrefix = @q || '%'`）；聚合结果并包含各类型计数；支持 `type` 过滤参数；按 `ts_rank` DESC 排序；禁止将 `ILIKE '%@q%'` 作为常规路径
- [ ] T038 `GET /api/v1/search/autocomplete?q=`：AutocompleteQuery——每种类型（movie/tv_series/anime/people）搜索前 3 条；Redis 缓存 `search:autocomplete:{q}` 5 分钟；返回带 `see_all_url` 的分组响应
- [ ] T039 `GET /api/v1/rankings`：RankingsQuery——热门榜（popularity DESC，每种类型 top 50，日常 Redis 缓存 `rankings:{type}:hot` 24 小时）；高分榜（douban_score DESC，每种类型 top 50，`rankings:{type}:score` 24 小时）；电影 Top100（douban_score ≥ 7.0 且 douban_rating_count ≥ 1000，前 100）

### 实施说明
- zhparser 可用性检查：在启动时尝试查询扩展，设置 `_zhparserAvailable` 标志，在整个服务生命周期中使用
- 跨 4 张表的全文搜索需要 UNION ALL——构建包含显式 content_type 判别列的联合查询
- 排行榜 Top100 门控：`WHERE douban_score >= 7.0 AND douban_rating_count >= 1000 AND deleted_at IS NULL AND status = 'published'`

### 依赖关系
- 依赖 WP02、WP03

---

## 第2阶段：管理后台 API

---

## 工作包 WP10：管理后台 API – 内容 CRUD（优先级：P3）

**目标**: 实现所有内容类型（Movie、TVSeries、Anime、Person、Franchise）的管理端点，支持创建、更新和软删除。
**独立验收测试**: 携带有效 JWT 和请求体的 `POST /api/v1/admin/movies` 创建一条 status=published 的电影记录。`DELETE /api/v1/admin/movies/1` 设置 `deleted_at` 而不是删除行。
**提示文件**: `tasks/WP10-admin-content-crud-api.md`
**预估规模**: 约 370 行

### 包含子任务
- [ ] T040 电影管理 CRUD：`POST /admin/movies`（CreateMovieCommand → 直接插入，status=published）、`PUT /admin/movies/:id`（UpdateMovieCommand → 更新 + 使 Redis 失效）、`DELETE /admin/movies/:id`（SoftDeleteCommand → 设置 deleted_at）
- [ ] T041 TVSeries 管理 CRUD：与电影相同的模式 + 季/集子资源（`POST /admin/tv/:id/seasons`、`POST /admin/tv/:id/seasons/:n/episodes`）
- [ ] T042 Anime 管理 CRUD：与 TV 相同的模式 + 动漫特有字段（origin、studio、source_material）
- [ ] T043 影人 + Franchise 管理 CRUD：`POST/PUT/DELETE /admin/people`（含 photos_cos_keys 数组管理）、`POST/PUT/DELETE /admin/franchises`
- [ ] T044 `GET /api/v1/admin/stats`：统计每种内容类型的已发布/草稿记录数；管理列表页的标题关键词搜索端点 `GET /admin/{type}?q=keyword`

### 实施说明
- 所有管理端点需要 `[Authorize]` 特性——JWT RS256 对 JWKS URI 验证
- 演职员管理：更新电影时，在请求体中接受 `credits[]` 数组；在 UoW 事务中删除该内容现有演职员，重新插入新演职员
- 软删除：仅设置 `deleted_at = NOW()`；管理 GET 端点必须支持 `?include_deleted=true` 参数显示软删除项目
- 验证：使用 FluentValidation 或 DataAnnotations；返回带字段级错误的 422

### 并行机会
- T040（电影）、T041（TV）、T042（Anime）、T043（影人/Franchise）完全并行

### 依赖关系
- 依赖 WP02、WP03

---

## 工作包 WP11：管理后台 API – 爬虫审核 + Banner 管理（优先级：P3）

**目标**: 实现 pending_content 审核工作流（通过/拒绝/重置/批量通过）及 Hero Banner CRUD。
**独立验收测试**: `POST /admin/pending/1/approve` 返回与 raw_data 字段匹配的预填充 DTO。带 `[1,2,3]` 的 `POST /admin/pending/bulk-approve` 在单个事务中更新所有 3 条记录。
**提示文件**: `tasks/WP11-admin-crawler-review-and-banner-api.md`
**预估规模**: 约 350 行

### 包含子任务
- [ ] T045 `GET /api/v1/admin/pending`：列出带 `review_status` 过滤器的 pending_content（pending/approved/rejected），分页，按 created_at DESC 排序；`GET /admin/pending/:id`：单条记录及格式化展示的 raw_data
- [ ] T046 `POST /admin/pending/:id/approve`：更新 review_status='approved' + reviewed_at；提取 raw_data 字段 → 映射到实体 DTO → 返回 `{prefilled_data: {...}, content_type}`，供管理前端跳转到编辑表单
- [ ] T047 `POST /admin/pending/:id/reject`：设置 review_status='rejected'；`POST /admin/pending/:id/reset`：设置 review_status='pending'，清除 reviewed_at
- [ ] T048 `POST /admin/pending/bulk-approve`：接受 `{ids: []}` 请求体；在单个 UoW 事务中循环通过；返回 `{approved_count, failed_ids}` 响应
- [ ] T049 Banner CRUD：`GET /admin/banners`（列出含 content_type/content_id、display_order、时间范围的记录）、`POST /admin/banners`（带验证创建）、`PUT /admin/banners/:id`（更新顺序/时间）、`DELETE /admin/banners/:id`（硬删除 Banner 配置，非内容）

### 实施说明
- `approve` 不自动发布内容——仅返回预填充数据。管理员随后须提交编辑表单（T040/T041/T042）才能创建实际发布记录
- raw_data → 实体字段映射因 content_type 而异：为 douban/mtime/tmdb 来源创建映射字典
- Banner `display_order`：允许间隔（如 10, 20, 30）以便于重新排序；前端按 display_order ASC 排序
- Banner 激活过滤：`WHERE (start_at IS NULL OR start_at <= NOW()) AND (end_at IS NULL OR end_at > NOW())`

### 依赖关系
- 依赖 WP10（管理模式已建立）

---

## 工作包 WP12：热度追踪 + 定时任务（优先级：P2）

**目标**: 实现页面浏览追踪端点、每日热度分数更新任务和每日排行榜缓存刷新任务。
**独立验收测试**: `POST /api/v1/tracking/view` 插入一条 page_views 记录。运行热度 cron 后，电影的 `popularity` 字段反映过去 7 天的 PV 计数。cron 后 Rankings Redis 键被刷新。
**提示文件**: `tasks/WP12-popularity-tracking-and-scheduled-tasks.md`
**预估规模**: 约 240 行

### 包含子任务
- [ ] T050 `POST /api/v1/tracking/view`：以 fire-and-forget 方式插入 `page_views (content_type, content_id, viewed_at)`；立即返回 204；不需要鉴权；按 IP 限速（每个内容项每分钟最多 10 次，防止滥用）
- [ ] T051 每日热度更新任务（cron：每日 02:00）：UPDATE movies/tv_series/anime/people SET popularity = (SELECT COUNT(*) FROM page_views WHERE content_type=X AND content_id=id AND viewed_at >= NOW()-7 DAYS)；使用 IHostedService 或 Hangfire
- [ ] T052 每日排行榜缓存刷新任务（cron：每日 02:30）：重新生成并 SET 所有 `rankings:*:hot` 和 `rankings:*:score` Redis 键；确保排行榜反映更新后的热度和新内容

### 实施说明
- 两个 cron 任务均使用 .NET `BackgroundService`（IHostedService）；使用 `NCrontab` 或 `Cronos` 库解析 cron 表达式
- page_views 表：添加复合索引 `(content_type, content_id, viewed_at)` 以支持高效的 7 天窗口 COUNT
- 热度更新应作为每张表的单次批量 UPDATE，而非逐行更新
- 排行榜刷新：查询 + 序列化为 JSON + `SET key JSON EX 86400`

### 依赖关系
- 依赖 WP04（API 模式）、WP03（Redis）

---

## 第3阶段：爬虫

---

## 工作包 WP13：Python Scrapy 爬虫（优先级：P3）

**目标**: 实现完整的 Scrapy 爬虫系统，包括 TMDB API 蜘蛛、豆瓣 HTML 解析器、时光网子评分解析器，以及去重 + PostgreSQL 写入管道。
**独立验收测试**: `scrapy crawl tmdb_spider -a content_type=movie -a ids=550` 插入 1 条 pending_content 记录。以相同 ID 重新运行不创建重复记录（按 source_url 去重）。
**提示文件**: `tasks/WP13-scrapy-crawler.md`
**预估规模**: 约 480 行

### 包含子任务
- [ ] T053 Scrapy 项目搭建：`scrapy startproject crawler`；配置 `settings.py`（DOWNLOAD_DELAY=3、RANDOMIZE_DOWNLOAD_DELAY=True、默认请求头、开发环境 HTTPCACHE）；`requirements.txt`（scrapy、psycopg2-binary、python-dotenv）
- [ ] T054 反爬虫中间件：`proxy_middleware.py`（从 settings 的 PROXY_LIST 循环代理，每次请求轮换）、`useragent_middleware.py`（从 20+ 个真实浏览器 UA 池中随机选择）；在 settings 中启用
- [ ] T055 去重管道：`dedup_pipeline.py`——插入前检查 `pending_content.source_url`；若已存在则跳过（记录 SKIP）；管道使用 psycopg2 直连（非 Django ORM）
- [ ] T056 PostgreSQL 写入管道：`postgres_pipeline.py`——INSERT INTO pending_content (source, source_url, content_type, raw_data)，带冲突处理；在 spider_closed 信号时关闭 DB 连接
- [ ] T057 TMDB 蜘蛛：`tmdb_spider.py`——使用 TMDB API v3（`/movie/{id}`、`/tv/{id}`）；将 TMDB 响应字段映射到与 `content_keywords` 匹配的 `raw_data` schema；通过 `ids` 参数处理批量导入的分页
- [ ] T058 豆瓣蜘蛛：`douban_spider.py`——解析豆瓣电影/电视剧/动漫 HTML；提取：title_cn、douban_score、douban_rating_count、douban_rating_dist（5 星分布）、synopsis；遵守 robots.txt
- [ ] T059 时光网蜘蛛：`mtime_spider.py`——解析时光网 HTML 获取子评分（音乐/视觉/导演/故事/表演）；通过 IMDB ID 或标题与内容匹配；存储为 `raw_data.mtime_scores`

### 实施说明
- TMDB 蜘蛛使用官方 API（免费密钥）——非 HTML 解析；速率限制：40 次请求/10 秒；使用 API v3 端点
- 豆瓣 + 时光网蜘蛛进行 HTML 解析——DOWNLOAD_DELAY 必须 ≥ 3 秒并加随机化，以避免被封锁
- `raw_data` 为 JSONB——按原样存储完整 API/解析响应；字段到实体字段的映射在管理员审核时完成（T046）
- 设置覆盖：`settings_local.py`（已加入 .gitignore）覆盖 `settings.py` 中的 API 密钥 + 代理列表

### 并行机会
- T055+T056（管道）可并行构建；T057、T058、T059（蜘蛛）完全并行

### 依赖关系
- 依赖 WP01（pending_content 表必须存在）

---

## 第4阶段：前端——公共组件

---

## 工作包 WP14：前端脚手架 + 公共组件（优先级：P1）🎯 MVP

**目标**: 初始化 Vue 3 + Vite + Tailwind CSS 前端项目，实现多页面共用的所有 UI 公共组件。
**独立验收测试**: `npm run dev` 启动开发服务器；当 `poster_cos_key` 为 null 时，`MediaCard` 显示回退占位符；`Lightbox` 支持键盘左右箭头导航并可打开/关闭。
**提示文件**: `tasks/WP14-frontend-scaffold-and-common-components.md`
**预估规模**: 约 420 行

### 包含子任务
- [ ] T060 Vue 3 + Vite 项目初始化：`npm create vue@latest frontend`（TypeScript strict、Vue Router、Pinia）；安装 Tailwind CSS v4、Axios；创建 `src/api/` Axios 客户端（从环境变量读取 base URL，包含错误拦截器）、`src/utils/cos.ts`（CDN URL 辅助工具）、`src/stores/` Pinia 配置
- [ ] T061 `MediaCard.vue`：2:3 宽高比海报图片（`object-cover`）、标题叠加层、年份徽章、评分徽章（当 `douban_score` 为 null 时隐藏——不显示占位文字）；`<img>` 必须包含语义化 `alt`（优先标题，其次类型+年份）；图片加载失败时显示灰色占位符；点击 → router-link 到详情页
- [ ] T062 `Pagination.vue`：上一页/下一页按钮，大范围时最多显示 7 个页码按钮并含省略号；emit `page-change` 事件；通过 `useRoute()`/`useRouter()` 集成 URL 查询参数
- [ ] T063 `FilterBar.vue`：扁平标签行布局（类豆瓣风格）；每行含标签 + 标签按钮；选中标签以橙色背景高亮；支持行内多选（可配置）；emit `filter-change` 携带当前选择；语言/评分下拉框使用独立的 `DropdownFilter.vue`
- [ ] T064 `Lightbox.vue`：全屏遮罩（fixed 定位，z-50，深色背景）；object-contain 图片显示；左右箭头按钮；键盘 `ArrowLeft`/`ArrowRight` 导航；`Escape` 关闭；prop：`images: string[]`、`initialIndex: number`；emit `close`
- [ ] T065 `ImageTabBlock.vue`：tabs 数组 prop（名称 + cos_keys 数组 + count）；count=0 时完全隐藏该 Tab（非禁用）；默认激活 = 第一个非空 Tab（「剧照」）；图片以水平滚动行显示；每张图 `alt` 使用语义化文案（内容标题 + 图片类型 + 序号）；点击任意图片 → 在该索引处打开 Lightbox

### 实施说明
- Tailwind v4 配置：在主 CSS 中使用 `@import "tailwindcss"`；配置内容路径
- `cosUrl()` 辅助工具：`${import.meta.env.VITE_COS_CDN_BASE}/${key}`——当 key 为 null/空时返回 null
- 当 `douban_score` 为 null 时，MediaCard 评分区域不得显示任何文字（规格要求——无「X人想看」或占位符）
- FilterBar 标签行：使用 CSS `flex-wrap` 在小屏幕上自然换行

### 并行机会
- T061–T065 均为独立组件，可并行构建

### 依赖关系
- 无（前端独立于后端启动）

---

## 工作包 WP15：布局、NavBar、SearchBar 与核心 Composables（优先级：P1）🎯 MVP

**目标**: 实现站点布局外壳（NavBar、Footer）、可展开搜索栏（含自动补全）以及可复用 composables（useFilters、useSearch、usePagination）。
**独立验收测试**: 输入时 SearchBar 显示分组结果的自动补全下拉框；按 Enter 导航到 `/search?q=...`；FilterBar 状态可正确序列化到 URL 查询参数及从其恢复。
**提示文件**: `tasks/WP15-layout-navbar-searchbar-composables.md`
**预估规模**: 约 360 行

### 包含子任务
- [ ] T066 `NavBar.vue`：站点 Logo（左侧）、导航链接（电影/电视剧/动漫/影人 → /movies, /tv, /anime, /people）、搜索图标按钮（切换搜索栏）；响应式：在移动端（`< 768px`）折叠导航链接为汉堡菜单；根据当前路由高亮激活链接
- [ ] T067 `Footer.vue`：含站点名称、简短描述的极简 footer；响应式布局
- [ ] T068 `SearchBar.vue`（打开时内联显示在 NavBar 下方）：带防抖（300ms）的输入框；调用 `GET /api/v1/search/autocomplete?q=`；显示下拉框：4 种类型区块（电影/电视剧/动漫/影人），每种最多 3 条结果，条目显示海报缩略图 + 标题 + 年份；底部「查看全部结果」链接；按 Enter 或点击「查看全部」→ 导航到 `/search?q=`；点击条目 → 导航到详情页；`useSearch` composable 处理所有获取逻辑
- [ ] T069 `useFilters.ts` composable：挂载时从 URL 查询参数读取 → 响应式过滤器状态对象；监听过滤器状态变化 → 以更新后的参数调用 `router.push()`；暴露 `activeFilters`、`setFilter(key, value)`、`clearFilters()`、`filterToQueryParams()`
- [ ] T070 `useSearch.ts` composable：防抖自动补全获取；`usePagination.ts`：从 URL `?page=` 读取页码状态，计算属性 `totalPages`、`prevPage/nextPage` 辅助方法

### 实施说明
- SearchBar 自动补全：点击外部时关闭（使用 VueUse 的 `onClickOutside` 或原生 blur 处理器）
- `useFilters` 必须处理数组值过滤器（genre 支持多选）——序列化为重复参数（`?genre=sci-fi&genre=action`）
- NavBar 链接使用 `router-link` 配合 `exact-active-class` 进行正确高亮

### 依赖关系
- 依赖 WP14（项目脚手架必须存在）

---

## 第5阶段：前端——内容页面

---

## 工作包 WP16：首页（前端）（优先级：P1）🎯 MVP

**目标**: 实现首页，包含 Hero Banner 自动轮播、各内容分类的横向滚动卡片列表，以及排行榜/奖项入口区块。
**独立验收测试**: Banner 每 5 秒自动轮播；Banner 列表为空时不渲染任何 Banner 区块；在 1280px 视口下，热门电影卡片显示 8+ 张 MediaCard 横向滚动，不触发页面横向滚动条。
**提示文件**: `tasks/WP16-home-page.md`
**预估规模**: 约 290 行

### 包含子任务
- [ ] T071 Hero Banner 轮播（`HeroBanner.vue`）：获取 `/api/v1/home` → `banners`；用 `setInterval` 每 5 秒自动轮播；手动圆点指示器导航；平滑过渡（CSS transition 或 Vue `<Transition>`）；当 `banners.length === 0` 时不渲染 `<section>`；背景图全宽铺满并带渐变遮罩
- [ ] T072 热门列表区块：`HorizontalScroll.vue` 包装组件（overflow-x auto，桌面端隐藏滚动条，移动端显示）；热门电影列表（8+ 张 MediaCard）、热门电视剧列表（8+ 张 MediaCard）；从 `/api/v1/home` 获取数据
- [ ] T073 热门动漫区块含 Tab：「国漫」/「日漫」切换按钮；从组合首页响应中过滤本地状态；每个 Tab 横向滚动显示 ≥8 张卡片
- [ ] T074 排行榜入口卡片（静态网格：「电影排行」「电视剧排行」「动漫排行」各自链接到 `/rankings?tab=movie|tv|anime`）；奖项入口卡片（奥斯卡、金球奖、戛纳等图文卡片链接到 `/awards/oscar` 等）

### 实施说明
- Banner 必须在 `onUnmounted()` 时清除 interval，避免内存泄漏
- 首页 API 调用失败时，显示优雅的空状态（无 Banner、无列表）——不崩溃页面
- 横向滚动：使用 `scroll-smooth` CSS，桌面端鼠标悬停时添加左右箭头按钮

### 依赖关系
- 依赖 WP14（MediaCard、公共组件）、WP15（NavBar 布局）

---

## 工作包 WP17：电影列表页（前端）（优先级：P1）🎯 MVP

**目标**: 实现 `/movies` 列表页，包含所有维度的 FilterBar、排序控件、分页网格，以及 URL ↔ 过滤状态双向同步。
**独立验收测试**: 选择类型「科幻」+ 地区「欧美」后，URL 更新为 `?genre=sci-fi&region=us`；分享该 URL 可在页面加载时恢复过滤状态；在 1280px 下无横向滚动条。
**提示文件**: `tasks/WP17-movie-list-page.md`
**预估规模**: 约 300 行

### 包含子任务
- [ ] T075 `/movies/index.vue` 结构：挂载时通过 `useFilters()` 从 URL 加载过滤器；以过滤参数调用 `GET /api/v1/movies`；显示带以下行的 `FilterBar`：类型标签（科幻/动作/爱情/恐怖/喜剧/纪录片/动画/剧情/犯罪/悬疑...）、地区标签（大陆/香港/台湾/美国/英国/日本/韩国/法国...）、年代标签（2020s/2010s/2000s/90s/更早）
- [ ] T076 语言下拉 + 评分下拉过滤器（普通话/粤语/英语/日语/韩语/其他；9分+/8分+/7分+/不限）；排序控件行（综合热度/豆瓣评分/最新上映）；结果网格（移动端 3 列 / 平板 4 列 / 桌面 6 列，最多 24 张卡片）
- [ ] T077 URL 同步：`useFilters()` composable（来自 WP15）；过滤器变化时 → `router.replace()` 合并参数（过滤器变化时重置 page=1）；页码变化时 → `router.push({query: {...currentFilters, page: n}})`
- [ ] T078 网格下方放置 Pagination 组件；加载骨架屏状态（API 获取期间显示灰色卡片占位符）；0 条结果时显示空状态提示信息

### 实施说明
- FilterBar 标签选中使用橙色背景（Tailwind：`bg-orange-500 text-white`）
- 行内多选：类型、地区、年代均支持多选；语言和评分为单选
- 过滤器变化时 API 调用必须防抖（300ms），避免多选过滤器时快速触发大量请求
- 新增单年份精确筛选控件（`year`），与年代筛选（`decade`）并存；当 `year` 与 `decade` 同时存在时，以 `year` 为准；参数格式保持 `year=2024`、`decade=2020s`

### 依赖关系
- 依赖 WP14（MediaCard、FilterBar、Pagination）、WP15（useFilters）

---

## 工作包 WP18：电影详情页（前端）（优先级：P1）🎯 MVP

**目标**: 实现 `/movies/[id]` 详情页，包含所有区块：Hero、评分条、演职员、预告片、奖项、系列块、相似内容、图片 Tab 块及完整演职员子页面。
**独立验收测试**: `<title>` 为「复仇者联盟2 (2015) - 影视网」；电影无系列时不渲染 franchise 块；简介超过 150 字符时折叠并显示「展开」切换；Lightbox 键盘导航正常；`/movies/1/credits` 页面渲染所有部门。
**提示文件**: `tasks/WP18-movie-detail-page.md`
**预估规模**: 约 410 行

### 包含子任务
- [ ] T079 Hero 区块（`MovieDetailHero.vue`）：背景图（全宽，模糊滤镜遮罩）、海报（2:3 比例）、基本信息（title_cn、title_original、年份、类型、地区、导演、主演前 5）；通过 `useHead()` 或直接 `document.title` 设置 `<title>` 标签；`<meta name="description">` 含简介摘录
- [ ] T080 评分区块（`RatingBlock.vue`）：豆瓣评分 + 5 星分布进度条（力荐/推荐/还行/较差/很差标签含百分比）；IMDB 评分徽章；时光网子评分（音乐/视觉/导演/故事/表演）——仅在数据存在时显示
- [ ] T081 演员网格（可点击头像 → `/people/[id]`）、简介（`SynopsisBlock.vue`——超过 150 字符时折叠，「展开/收起」切换）、视频区块（按类型分 Tab：正式预告/花絮/幕后/片段——嵌入 iframe 或 YouTube 链接）
- [ ] T082 奖项块（`AwardBlock.vue`——获奖用金色图标，提名用灰色，超过 5 条时折叠并显示数量链接）；系列块（`FranchiseBlock.vue`——仅在 `franchise != null` 时渲染，显示系列名链接 + 「第N部 / 共X部」）；相似内容行（6 张 MediaCard，数组为空时不渲染）
- [ ] T083 图片 Tab 块（复用 WP14 的 `ImageTabBlock.vue`：剧照 + 海报 Tab，默认显示剧照，Tab 标签显示数量，隐藏空 Tab）；页面级布局和响应式断点验证
- [ ] T126 完整演职员页面（`/movies/[id]/credits`）：获取 `GET /api/v1/movies/:id/credits`；面包屑（电影名 → 全部演职员）；`<title>` 格式：`{titleCn} 全部演职员 - 影视网`；每个部门一个 `<section>`（导演/编剧/主演/制片人/其他）；60×60px 圆形头像链接到 `/people/:id`；桌面端 2 列网格，移动端 1 列；演职员为零的部门隐藏

### 实施说明
- 背景虚化：在背景图上使用 `filter: blur(8px); transform: scale(1.1)` + 深色遮罩（`bg-black/60`）——不使用 CSS backdrop-filter（Safari 兼容性问题）
- SPA 模式下使用 `useHead()` 或 Vite 插件 `vite-plugin-document-title` 管理 `<title>`
- 演员网格：通过 `/movies/:id` 端点数据显示前 20 人；「查看全部演职员」链接 → `/movies/:id/credits`（T126）
- T126 演职员页面：头像回退为灰色圆形占位符；移动端 = 单列，桌面端 = 每部门 2 列网格

### 依赖关系
- 依赖 WP14（ImageTabBlock、Lightbox、MediaCard）、WP15（布局）

---

## 工作包 WP19：电视剧列表 + 详情页（前端）（优先级：P2）

**目标**: 实现 `/tv` 列表页（含 air_status 过滤器）、`/tv/[id]` 详情页（含下一集块和季折叠面板）以及 `/tv/[id]/season/[n]` 季详情页。
**独立验收测试**: 季折叠面板默认展开最新季；折叠一季时隐藏集列表但保留季头可见；仅在剧集播出中且 next_episode_info 存在时显示下一集块。
**提示文件**: `tasks/WP19-tv-series-pages.md`
**预估规模**: 约 380 行

### 包含子任务
- [ ] T084 `/tv/index.vue`：TV 列表页——与电影列表相同的 FilterBar + 额外的 air_status 行（全部/连载中/已完结/制作中/已取消，多选）；卡片显示 air_status 标签徽章（播出中绿色，已完结灰色）；所有 URL 同步和排序控件
- [ ] T085 `/tv/[id].vue` Hero 区块 + 下一集块 + 奖项区块：Hero 与电影详情相同模式；`NextEpisodeBlock.vue`——仅在 `air_status === 'airing' && next_episode_info != null` 时渲染；显示预计播出日期 + 剧集标题；新增 `AwardBlock.vue`（获奖金色、提名灰色，超过 5 条折叠）并接入 TV 详情 awards 数据
- [ ] T133 `/tv/[id]/credits` 页面：面包屑、分组区块、头像跳转 `/people/:id`，`<title>`=`{titleCn} 全部演职员 - 影视网`
- [ ] T135 `/tv/[id].vue` 图片区块：接入 `ImageTabBlock.vue`，支持「剧照/海报」Tab，默认「剧照」，Tab 显示数量；空类别 Tab 隐藏；点击图片进入全屏灯箱，支持左右箭头与键盘方向键翻页
- [ ] T136 `/tv/[id].vue` 相似内容区块：调用 `GET /api/v1/tv/:id/similar`，展示约 6 张 MediaCard（封面、标题、年份、评分）；无结果时区块不渲染
- [ ] T086 `SeasonAccordion.vue`：季折叠面板列表；默认展开 = 最高 season_number；折叠状态显示：季海报缩略图（null 时灰色占位符）、季名称、集数、首播日期、平均评分（可用时）、简介截断至 3 行（CSS `-webkit-line-clamp: 3`）含 `…`；展开状态：海报 + 简介保持可见，集列表显示在下方；集行：S{season_number}E{episode_number} 代码、标题、播出日期、时长、still_cos_key 缩略图
- [ ] T087 `/tv/[id]/season/[n].vue`：面包屑（剧名 → 第N季）；季头（完整简介、海报、统计）；按 episode_number 排序的完整集列表；页面底部上一季/下一季导航（第1季无上一季，最新季无下一季）

### 实施说明
- 季折叠面板使用 `v-show`（非 `v-if`）展示展开内容，避免切换时重渲染闪烁
- 面包屑：剧名为 `<router-link>` 链接到 `/tv/:id`；面包屑不需要 JavaScript（静态计算属性）
- 剧照图片占位符：与剧照图片相同宽高比的灰色 `bg-gray-200` div
- 新增单年份精确筛选控件（`year`），与年代筛选（`decade`）并存；当 `year` 与 `decade` 同时存在时，以 `year` 为准；参数格式保持 `year=2024`、`decade=2020s`

### 依赖关系
- 依赖 WP14（ImageTabBlock、MediaCard）、WP15（布局）

---

## 工作包 WP20：动漫列表 + 详情页（前端）（优先级：P2）

**目标**: 实现 `/anime` 列表页（含产地 Tab 和来源类型过滤器）、`/anime/[id]` 详情页（含制作公司/产地块和配音演员演职员）以及 `/anime/[id]/season/[n]`。
**独立验收测试**: 点击「国漫」Tab 更新 URL `?origin=cn` 并仅显示国漫；配音演员卡片在演员名旁显示角色名。
**提示文件**: `tasks/WP20-anime-pages.md`
**预估规模**: 约 310 行

### 包含子任务
- [ ] T088 `/anime/index.vue`：产地 Tab 行（全部/国漫/日漫 始终置顶显示，点击 → 更新 `origin` 查询参数）；FilterBar：与基础过滤器相同 + 独立的来源类型行（原创/漫画改编/小说改编/游戏改编）；卡片徽章显示产地标签 + 来源类型标签
- [ ] T134 `/anime/[id]/credits` 页面：与 TV credits 页面结构一致，保留配音角色信息，`<title>`=`{titleCn} 全部演职员 - 影视网`
- [ ] T089 `/anime/[id].vue`：动漫详情页——Hero 区块 + 制作信息块（`StudioBlock.vue`：制作公司名、来源类型标签、产地标签「国漫/日漫/其他」）；演职员区块将配音演员（在演员名下方显示 character_name）与导演/制作人员分开；复用 SeasonAccordion；复用 ImageTabBlock；相似内容；新增奖项区块 `AwardBlock.vue`（获奖金色、提名灰色，超过 5 条折叠）并接入 Anime 详情 awards 数据
- [ ] T090 `/anime/[id]/season/[n].vue`：动漫季详情——与 TV 季详情页结构完全相同；面包屑链接到 `/anime/[id]`；`<title>` 格式：`{动漫名} 第{N}季 - 影视网`

### 实施说明
- `StudioBlock.vue`：仅当 studio/source_material/origin 中至少有一个有值时显示
- 配音演员卡片：两行布局（上行：演员名；下行：「配音：{character_name}」小字）
- 复用 WP19 的 `SeasonAccordion.vue`——从动漫详情响应传入季数据
- 新增单年份精确筛选控件（`year`），与年代筛选（`decade`）并存；当 `year` 与 `decade` 同时存在时，以 `year` 为准；参数格式保持 `year=2024`、`decade=2020s`

### 依赖关系
- 依赖 WP14、WP15、WP19（复用 SeasonAccordion 组件）

---

## 工作包 WP21：影人、Franchise 与奖项页面（前端）（优先级：P2）

**目标**: 实现影人详情页（照片墙 + 合作者）、Franchise 详情页，以及奖项主页 + 届次详情页。
**独立验收测试**: 照片墙以网格布局渲染并支持 Lightbox；`photos_cos_keys` 为空时照片墙区块隐藏；Franchise 页电影按 franchise_order 排序（序号标签可见）；奖项提名中获奖者以金色高亮显示。
**提示文件**: `tasks/WP21-person-franchise-awards-pages.md`
**预估规模**: 约 360 行

### 包含子任务
- [ ] T091 `/people/[id].vue`：个人资料区块（头像、name_cn/en、职业标签、出生/死亡/国籍/身高）；传记超过 200 字符时可折叠；新增获奖记录区块（按颁奖年份 DESC，含奖项名、届次、类别、is_winner，超过 5 条可折叠，获奖金色/提名灰色）；作品 Tab 栏（全部/导演/编剧/演员）——每个 Tab 显示含年份 + 角色信息的内容卡片；演员 Tab 中在标题下方显示 character_name
- [ ] T092 `CollaboratorBlock.vue`（top-8 合作者，每人显示头像 + 姓名 + 「合作N次」）；`PhotoWall.vue`（网格布局 `grid-cols-4 md:grid-cols-5`，点击打开 Lightbox，区块标题显示「写真 (N)」，数组为空时整个区块不渲染）
- [ ] T093 `/franchises/[id].vue`：页面标题（系列名 + 「共N部」）；简介超过 200 字符时可折叠；电影列表按 franchise_order ASC 排序，每条：「第N部」序号标签、海报、标题、年份、douban_score（null 时显示「暂无评分」）；点击 → `/movies/[id]`；`<title>` 格式：`{系列名} - 影视网`
- [ ] T094 `/awards/[slug].vue`（奖项主页：赛事信息、届次列表链接）+ `/awards/[slug]/[edition].vue`（提名按类别分组：每个类别一个 `<section>`，提名列表含海报 + 标题链接 + 影人链接 + 金色/灰色 is_winner 图标）；上一届/下一届导航

### 实施说明
- 作品 Tab 过滤：从完整作品列表进行客户端过滤（非每个 Tab 单独 API 调用）——首次获取后将作品数据保存在 Pinia store 中
- 照片墙网格：使用 CSS Grid，每格 `aspect-square` 保持统一尺寸；图片 object-fit: cover
- 奖项提名在类别内排序：获奖者优先（is_winner=true 置顶），其余为提名

### 依赖关系
- 依赖 WP14（Lightbox、MediaCard）、WP15（布局）

---

## 工作包 WP22：搜索 + 排行榜 + SEO（前端）（优先级：P2）

**目标**: 实现 `/search` 搜索结果页和 `/rankings` 排行榜页，以及全站 SEO 优化（title 标签、meta 描述、懒加载）。
**独立验收测试**: 搜索 `?q=星际` 显示带计数的分 Tab 结果；「电影」Tab 激活；0 条结果的 Tab 显示为灰色且不可点击；排行榜页面前 3 名显示金/银/铜徽章。
**提示文件**: `tasks/WP22-search-rankings-seo.md`
**预估规模**: 约 300 行

### 包含子任务
- [ ] T095 `/search/index.vue`：从 URL 读取 `?q=`；调用 `GET /api/v1/search?q=`；显示 Tab 栏（全部/电影/电视剧/动漫/影人 含计数徽章）；无效 Tab（count=0）使用 `disabled` 样式（灰色，`cursor-not-allowed`）；结果卡片：海报缩略图 + 标题 + 年份 + 类型徽章 + 简介前 60 字符；空状态：「未找到与「{q}」相关的内容」
- [ ] T096 `/rankings/index.vue`：内容类型 Tab（电影/电视剧/动漫）；子 Tab（热门榜/高分榜）；电影 Tab 还显示 Top100 入口；排名列表：排名徽章（1=金，2=银，3=铜；4+=普通数字）、MediaCard 风格行含排名 + 海报 + 标题 + 年份 + 评分；Top100 门控显示说明（「豆瓣评分 ≥ 7.0，评分人数 ≥ 1000」）
- [ ] T097 SEO 优化：实现 `usePageMeta(title, description)` composable，在每个页面设置 `document.title` 和 `<meta name="description">`；确保所有详情页按规格中的正确格式字符串调用它；添加 `sitemap.xml` 生成脚本（可选）
- [ ] T098 图片性能：对 MediaCard、剧照、照片墙中的所有 `<img>` 添加 `loading="lazy"`；Hero/Banner 图片使用 `loading="eager"` + `fetchpriority="high"`；对所有图片添加 `onerror` 回退处理器（显示灰色占位符 div）

### 实施说明
- 搜索 Tab 激活状态：从 URL 读取 `?type=movie|tv|anime|person` + 点击 Tab 时更新
- 排行榜页面：在单次 `/rankings` API 调用中组合热门榜（来自 `popularity`）和高分榜（来自 `douban_score`）数据
- Top100 徽章：在电影高分榜条目上显示 `<span class="badge">Top 100</span>`

### 依赖关系
- 依赖 WP14、WP15

---

## 第6阶段：管理前端

---

## 工作包 WP23：管理前端脚手架 + 仪表盘 + 电影 CRUD（优先级：P3）

**目标**: 初始化 Vue 3 + TDesign Vue 管理项目，实现 OAuth 2.0 PKCE 登录流程、仪表盘统计页面及电影内容创建/编辑/列表页。
**独立验收测试**: 未携带 token 访问 `/admin` 时跳转到 OAuth 登录；登录后仪表盘显示内容计数；电影创建表单在提交前验证必填字段（title_cn）。
**提示文件**: `tasks/WP23-admin-scaffold-dashboard-movie-crud.md`
**预估规模**: 约 380 行

### 包含子任务
- [ ] T099 管理后台 Vue 3 + TDesign Vue 项目：`npm create vue@latest admin`（TypeScript）；安装 `tdesign-vue-next`、Axios、Pinia；配置路由（`beforeEach` 鉴权守卫，检查 localStorage 中的 JWT → 若不存在则跳转到 `/login`）；实现 `/login` 页面，含 OAuth 2.0 PKCE 流程（跳转到提供方 → 回调 → 交换 code → 存储 JWT）
- [ ] T100 管理布局（`AdminLayout.vue`）：TDesign `t-layout` 含侧边栏导航（内容管理/爬虫审核/Banner管理 各区块）+ 带登出按钮的顶部栏；仪表盘页面（`/admin`）：调用 `GET /admin/stats`，使用 `t-card` 组件显示计数（电影 N 部 / 电视剧 N 部 / 动漫 N 部 / 影人 N 人 / 待审核 N 条）
- [ ] T101 电影列表页（`/admin/content/movies`）：TDesign `t-table` 含列（ID、title_cn、status、created_at、操作）；标题输入框搜索；软删除切换开关；每行「编辑/删除」操作按钮；删除时显示确认对话框
- [ ] T102 电影创建/编辑表单（`/admin/content/movies/new`、`/admin/content/movies/:id/edit`）：MovieDetailDto 中的所有字段；带验证的 `t-form`；演职员区块（可搜索影人下拉 + 角色下拉 + character_name 输入，支持添加/删除行）；Franchise 下拉（可搜索）；奖项区块（添加/删除提名记录）；提交 → 调用 POST/PUT 管理 API

### 实施说明
- OAuth PKCE：生成 `code_verifier`（随机 43-128 字符），`code_challenge = BASE64URL(SHA256(code_verifier))`；在跳转期间将 verifier 存入 sessionStorage；在回调时交换
- JWT 存储：使用 `localStorage`（或更严格的 XSS 防护使用 `sessionStorage`）；通过 Axios 拦截器在所有 API 请求中包含 `Authorization: Bearer {token}`
- 电影表单演职员管理：通过姓名搜索获取影人（`GET /admin/people?q=`）用于自动补全

### 依赖关系
- 无（管理前端独立启动）

---

## 工作包 WP24：管理前端 – TV/Anime/影人/Franchise CRUD（优先级：P3）

**目标**: 实现电视剧（含季/集管理）、Anime、影人和 Franchise 的管理 CRUD 页面。
**独立验收测试**: 创建含 2 季、每季 5 集的电视剧可通过 API 正确持久化；影人表单上传照片并显示预览；Franchise 表单显示有序电影列表。
**提示文件**: `tasks/WP24-admin-tv-anime-person-franchise-crud.md`
**预估规模**: 约 360 行

### 包含子任务
- [ ] T103 电视剧列表 + 创建/编辑表单：与电影相同的列表模式；编辑表单添加「季管理」Tab（内联添加/编辑/删除季 + 集）；季行：season_number、名称、first_air_date、海报上传；集表格：episode_number、名称、air_date、时长、剧照上传；主表单内的嵌套表单
- [ ] T104 Anime 列表 + 创建/编辑表单：与 TV 表单相同 + 动漫特有字段区块（产地单选组：国漫/日漫/其他；来源类型下拉；制作公司文本输入）；季/集管理与 TV 相同
- [ ] T105 影人列表 + 创建/编辑表单：头像上传（单图）+ 照片上传（多图画廊含预览和删除）；职业复选框；family_members 动态行（姓名 + 关系对）；传记文本域；IMDB ID 字段
- [ ] T106 Franchise 列表 + 创建/编辑表单：name_cn/en、简介文本域；关联电影区块（列出 franchise_id=当前的电影，通过拖拽或数字输入调整 franchise_order，添加/移除关联）

### 实施说明
- 图片上传：通过管理 API 端点上传到 COS（`POST /admin/upload`）→ 接收 cos_key → 存入表单字段
- 季/集嵌套表单：使用 TDesign `t-collapse` 实现季折叠面板；集表格支持内联编辑
- Franchise 电影关联：显示电影搜索 + 当前关联列表；franchise_order 在同一 Franchise 内必须唯一

### 依赖关系
- 依赖 WP23（管理脚手架 + 模式已建立）

---

## 工作包 WP25：管理前端 – 爬虫审核 + Banner 管理（优先级：P3）

**目标**: 实现待审内容审核列表 + 通过/拒绝流程，以及 Hero Banner 管理页面。
**独立验收测试**: 点击待审条目的「通过」按钮跳转到预填了 raw_data 字段的编辑表单；选中 3 条记录点击「批量通过」显示含计数的成功提示；Banner 列表按 display_order 排序显示。
**提示文件**: `tasks/WP25-admin-crawler-review-and-banner.md`
**预估规模**: 约 340 行

### 包含子任务
- [ ] T107 爬虫审核列表（`/admin/crawler`）：TDesign 表格含列（来源、content_type、raw_data 预览/标题、review_status 徽章、created_at）；状态 Tab 过滤器（待审核/已通过/已拒绝）；批量选择复选框列；「批量通过」按钮（选中 ≥1 条时激活）
- [ ] T108 审核详情 + 通过流程：点击行 → 导航到 `/admin/crawler/:id`，并排显示 raw_data 格式化字段与实体字段预览；「通过」按钮 → 调用通过 API → 接收 `{prefilled_data, content_type}` → 跳转到相应创建表单（`/admin/content/{type}/new`），表单从 prefilled_data 预填充
- [ ] T109 拒绝 + 重置工作流：列表和详情中的「拒绝」按钮 → 调用拒绝 API → 更新徽章；对已拒绝条目显示「重置为待审核」按钮 → 调用重置 API；两者均在不完整页面刷新的情况下更新 TDesign 表格中的行状态
- [ ] T110 Banner 管理（`/admin/banner`）：表格含列（content_type/id 预览、display_order、start_at/end_at 时间范围选择器、操作）；「新增」按钮 → 对话框含内容类型选择器（按标题搜索电影/TV/动漫）+ display_order 输入 + 时间范围选择器；内联或通过对话框编辑；删除含确认；时间范围使用 `t-date-range-picker`

### 实施说明
- 审核列表中的 raw_data 预览：提取 `raw_data.title_cn`（或 `raw_data.title`）用于预览列
- 预填充跳转：在跳转前将 prefilled_data 存入 Pinia store；新建表单页面在挂载时从 store 读取，然后清除
- Banner 时间范围：start_at 和 end_at 均为可选——空值表示「立即生效 / 永久有效」；两者均为 null 时显示「永久」标签

### 依赖关系
- 依赖 WP23（管理脚手架 + 模式）

---

## 第7阶段：可观测性与部署

---

## 工作包 WP26：可观测性 + 部署配置（优先级：P2）

**目标**: 添加 Sentry 错误追踪（后端 + 前端）、Prometheus 指标，配置 Nginx，并提供 Docker Compose 本地开发环境及 CI/CD 流水线存根。
**独立验收测试**: 触发未处理异常时向 Sentry DSN 发送事件。`curl http://localhost:5001/metrics` 返回 Prometheus 文本格式，含 HTTP 请求耗时直方图。`docker compose up` 启动所有服务并通过健康检查。
**提示文件**: `tasks/WP26-observability-and-deployment.md`
**预估规模**: 约 350 行

### 包含子任务
- [ ] T111 Sentry 集成：后端（`Sentry.AspNetCore` NuGet，Program.cs 中调用 `UseSentry()`，从 JWT Claims 捕获用户上下文）；前端（`@sentry/vue` npm，`main.ts` 中对前端和管理两个应用调用 `Sentry.init()`）；在上线前于 staging 环境验证错误捕获
- [ ] T112 Prometheus 指标：添加 `prometheus-net.AspNetCore` NuGet；暴露 `/metrics` 端点（按路由+状态码统计的 HTTP 请求数、耗时直方图、活跃连接数）；基础 HTTP 监控的 Grafana 仪表盘模板（JSON 导入）；在 README 中记录抓取配置
- [ ] T113 Nginx 配置：上游 `api` 块（localhost:5001）；`location /api/` → proxy_pass 含 `proxy_set_header`；`location /` → 提供 `/frontend/dist` 静态文件（SPA 路由的 try_files 回退）；`location /admin/` → 提供 `/admin/dist`；gzip 压缩；静态资源浏览器缓存（1 年）vs HTML（no-store）
- [ ] T114 Docker Compose（`docker-compose.yml`）：服务——`postgres`（postgres:15 + 预安装 zhparser 镜像）、`redis`（redis:7-alpine）、`api`（.NET API 镜像）、`frontend`（Nginx 提供构建后的前端）、`admin`（Nginx 提供构建后的管理）；健康检查；持久数据卷挂载；`.env` 文件存储机密
- [ ] T115 CI/CD 流水线与质量门禁：`build-and-test.yml` 包含 4 个 job（dotnet、frontend、admin、crawler），并增加强制门禁：`dotnet build -warnaserror` + `dotnet test`（覆盖率阈值）；`frontend`/`admin` 执行 `tsc --noEmit` 与 ESLint；任一检查失败即阻断合并；`deploy.yml` 负责镜像构建与部署
- [ ] T143 文档与注释合规门禁：后端启用 XML 文档生成并将缺失公开 API 注释（CS1591）纳入失败门禁；新增 PR 检查项要求“新功能更新 README/接口文档”；涉及架构级决策的工作包必须新增或更新 `docs/adr/` 记录
- [ ] T139 跨浏览器冒烟验收：在 Chrome/Edge/Safari（iOS）验证核心路径（首页→列表筛选→详情→搜索→排行榜→管理登录）可用；记录兼容性差异与修复清单
- [ ] T140 管理效率验收：基于固定样本集（100 条 `pending_content`）执行「待审内容→通过并预填充→编辑发布」全流程，输出自动化计时报告；验收阈值：平均单条处理耗时 ≤ 45 秒，批量通过吞吐率 ≥ 80 条/10 分钟
- [ ] T141 搜索相关性验收：构建固定中文查询集（不少于 30 条，覆盖片名/别名/人名/短词），验证 FTS 主路径与前缀回退命中质量并输出评测脚本结果；验收阈值：目标结果进入 Top5 的命中率 ≥ 80%
- [ ] T142 SLA/SLO 验收口径：建立上线前核对清单（可用性 99.9%、搜索 P95 ≤ 500ms、列表切换 ≤ 300ms、LCP ≤ 2.5s），定义采集方式、告警阈值与回滚触发条件
- [ ] T144 SEO 验收自动化：在 CI 或独立脚本中对核心详情页（电影/电视剧/动漫/影人/系列/奖项）运行移动端 Lighthouse SEO 审计，输出报告并校验阈值（抽样均值 ≥ 75，单页不低于 70）
- [ ] T145 性能基线验收：提供可复现压测/回放脚本，验证搜索接口 P95 ≤ 500ms、列表筛选切换 ≤ 300ms，并输出基线报告供发布门禁使用
- [ ] T146 缓存与发布验证：校验详情/列表/排行榜缓存 TTL 与主动失效行为，验证 CDN 缓存策略（静态资源长期缓存、HTML no-store）符合上线口径
- [ ] T147 并发容量验收：在预发布环境执行 10 分钟阶梯压测（1000→3000→5000 并发），校验 5000 并发阶段错误率 < 1%，且搜索接口 P95 ≤ 500ms、列表查询 P95 ≤ 300ms；同时验证 API 多实例扩展与 Redis 缓存命中生效，产出压测报告并纳入发布门禁

### 实施说明
- Sentry 前端：配置 `tracesSampleRate: 0.1`（10% 性能追踪）以控制成本
- .NET Docker 镜像：运行时使用 `mcr.microsoft.com/dotnet/aspnet:10.0`，构建阶段使用 `sdk:10.0`（多阶段 Dockerfile）
- PostgreSQL + zhparser：构建自定义 Docker 镜像 `FROM postgres:15` + 从源码编译 zhparser，或使用 `registry.cn-hangzhou.aliyuncs.com/zhparser/zhparser-pg15`（若可用）

### 依赖关系
- 依赖 WP02（后端脚手架）、WP14（前端脚手架）——但一旦这些工作包启动即可并行进行

---

## 依赖与执行摘要

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

WP27 (Backend xUnit Tests) [depends on WP02-WP11]
WP28 (Frontend Vitest Tests) [depends on WP14-WP18]
WP29 (Crawler pytest Tests) [depends on WP13]
```

**并行化亮点**：
- 后端（WP02–WP12）和前端（WP14–WP22）可同时构建
- 管理端（WP23–WP25）与主前端完全独立
- WP03 完成后，WP06、WP07、WP08、WP09、WP10 均可并行构建
- WP15 完成后，WP16–WP22 均可并行构建

**MVP 范围（第1阶段）**：WP01 → WP02 → WP03 → WP04 → WP05 → WP14 → WP15 → WP16 → WP17 → WP18

---

## 第8阶段：测试

---

## 工作包 WP27：后端 xUnit 测试（优先级：P2）

**目标**: 针对领域实体、应用服务和 API 控制器的 xUnit 测试套件。覆盖率门控 ≥ 80%；核心业务路径（鉴权、通过/拒绝、软删除）达到 100%。
**独立验收测试**: `dotnet test api/tests/ --collect:"XPlat Code Coverage"` 通过，覆盖率 ≥ 80% 且零测试失败。
**提示文件**: `tasks/WP27-backend-xunit-tests.md`
**预估规模**: 约 380 行

### 包含子任务
- [ ] T116 xUnit 项目配置（Unit + Integration 项目，Testcontainers PostgreSQL + Redis，`WebApplicationFactory`，覆盖率配置）
- [ ] T117 领域实体单元测试（Movie/TvSeries/Anime 软删除，PendingContent 通过/拒绝/重置，FeaturedBanner 激活时间逻辑）
- [ ] T118 应用服务单元测试（MovieApplicationService 缓存命中/未命中，PendingContentService 批量通过，SearchService zhparser 回退）
- [ ] T119 仓储集成测试（genre 数组 `&&` 过滤器，软删除可见性，Redis DeletePattern）
- [ ] T120 API 控制器集成测试（所有管理路由的 401 守卫，软删除往返，空查询搜索）

### 实施说明
- 使用 Testcontainers（`Testcontainers.PostgreSql`、`Testcontainers.Redis`）——不使用 InMemoryDatabase
- `TestJwtFactory.cs` 为鉴权测试生成一次性 RS256 JWT
- CI 门控：`dotnet test /p:Threshold=80 /p:ThresholdType=line`

### 依赖关系
- 依赖 WP02、WP03、WP04、WP05、WP10、WP11（所有业务逻辑必须先实现）

---

## 工作包 WP28：前端 Vitest 组件测试（优先级：P2）

**目标**: 针对章程规定的所有关键组件（MediaCard、FilterBar、Pagination）以及 BannerCarousel 和 SearchBar 的 Vitest 组件测试套件。
**独立验收测试**: 在 `/frontend` 中运行 `npm run test` 零失败通过；`npm run test:coverage` 显示组件覆盖率。
**提示文件**: `tasks/WP28-frontend-vitest-component-tests.md`
**预估规模**: 约 360 行

### 包含子任务
- [ ] T121 Vitest + @vue/test-utils 配置（jsdom 环境，全局路由存根，coverage v8，npm scripts）
- [ ] T122 MediaCard + ImageTabBlock 测试（null 评分隐藏，图片错误占位符，Tab 可见性）
- [ ] T123 FilterBar + Pagination 测试（标签高亮，emit 载荷，省略号，禁用上一页/下一页）
- [ ] T124 SearchBar 自动补全测试（使用假计时器的 300ms 防抖，下拉框，Enter 导航）
- [ ] T125 BannerCarousel + SynopsisBlock 测试（空时不渲染，5s 间隔，卸载时 clearInterval，150 字符折叠）

### 实施说明
- 在需要的组件上添加 `data-testid` 特性（已预期）
- 对所有计时器测试使用 `vi.useFakeTimers()` + `afterEach(() => vi.useRealTimers())`
- 在 `tests/setup.ts` 中模拟 COS base URL：`import.meta.env.VITE_COS_CDN_BASE = 'https://test-cdn.example.com'`

### 依赖关系
- 依赖 WP14（组件必须存在）、WP15（SearchBar）、WP16（BannerCarousel）、WP18（SynopsisBlock）

---

## 工作包 WP29：爬虫 pytest 测试（优先级：P2）

**目标**: 针对所有 Scrapy 管道和蜘蛛提取逻辑的 pytest 测试套件。所有测试在无真实 HTTP 请求或 DB 连接的情况下运行；CI 任务 `crawler pytest` 通过。
**独立验收测试**: `cd crawler && pytest --cov=douban tests/` 零失败通过；`pytest --collect-only` 显示所有测试条目无导入错误。
**提示文件**: `tasks/WP29-crawler-pytest-tests.md`
**预估规模**: 约 330 行

### 包含子任务
- [ ] T127 pytest 配置 + fixtures（`conftest.py` 含将 HTML 文件包装为 `HtmlResponse` 的 `fake_response()` 辅助工具、`mock_db` psycopg2 fixture、`pytest.ini`、3 个最小 HTML fixture 文件）
- [ ] T128 蜘蛛提取测试——电影（标题、评分、类型、content_type、source_url）、TV（air_status、first_air_date）、动漫（origin cn/jp）
- [ ] T129 管道单元测试——`DeduplicationPipeline`（放行新条目，丢弃重复，允许已拒绝条目重新提交）、`PostgresPipeline`（INSERT 语句、commit、连接关闭、JSON 序列化）
- [ ] T130 设置 + 中间件测试——`test_download_delay_at_least_3()` FR-28 回归守卫、`ROBOTSTXT_OBEY`、管道排序断言、UA 轮换中间件

### 实施说明
- `conftest.py` 中的 `fake_response()`：从 `tests/fixtures/` 读取 fixture 文件，包装为 Scrapy `HtmlResponse`——无真实 HTTP
- `mock_db` fixture 通过 `pytest-mock` 对 `douban.pipelines.psycopg2.connect` 打补丁；模拟链：`conn.cursor().fetchone.return_value = (0,)`
- T130 `test_download_delay_at_least_3()` 是回归守卫：导入 `douban.settings` 并断言 `DOWNLOAD_DELAY >= 3`；CI 会立即发现意外将 `DOWNLOAD_DELAY` 改为 1 的回归
- `tests/fixtures/` 中的 fixture HTML 文件应尽量精简——仅包含蜘蛛 CSS 选择器所需的 HTML 标签

### 依赖关系
- 依赖 WP13（Scrapy 爬虫必须在测试编写前存在）

---

## 子任务索引（参考）

| 子任务ID | 摘要 | 工作包 | 优先级 | 可并行？ |
|----------|------|--------|--------|---------|
| T001 | 初始化 monorepo 目录结构 | WP01 | P0 | 是 |
| T002 | 迁移：movies、tv_series、anime 表 | WP01 | P0 | 否 |
| T003 | 迁移：季/集表 | WP01 | P0 | 否 |
| T004 | 迁移：people、credits、franchises、keywords | WP01 | P0 | 否 |
| T005 | 迁移：media_videos、奖项、banners、pending、page_views | WP01 | P0 | 否 |
| T006 | 配置 zhparser FTS | WP01 | P0 | 否 |
| T007 | .NET 解决方案结构（4 层） | WP02 | P0 | 否 |
| T008 | 带 SqlSugar 特性的领域实体 | WP02 | P0 | 是 |
| T009 | IRepository 接口 + SqlSugar 基础实现 | WP02 | P0 | 否 |
| T010 | SqlSugar DI + UnitOfWork | WP02 | P0 | 否 |
| T011 | Application 层脚手架 + 基础 DTO | WP02 | P0 | 否 |
| T012 | Redis 缓存服务 + CacheKeys | WP03 | P0 | 是 |
| T013 | COS 存储客户端 | WP03 | P0 | 是 |
| T014 | 全局中间件（异常、日志、CORS、Swagger） | WP03 | P0 | 否 |
| T015 | OAuth 2.0 JWT RS256 鉴权 | WP03 | P0 | 否 |
| T016 | Sentry + Prometheus 集成 | WP03 | P0 | 否 |
| T017 | GET /home 端点 | WP04 | P1 | 是 |
| T018 | GET /movies 列表端点（含所有过滤器） | WP04 | P1 | 是 |
| T019 | 数组过滤 SQL 辅助工具 + 年代范围 | WP04 | P1 | 否 |
| T020 | Redis 缓存失效策略 | WP04 | P1 | 否 |
| T021 | GET /movies/:id 完整详情 DTO | WP05 | P1 | 是 |
| T022 | GET /movies/:id/credits | WP05 | P1 | 是 |
| T023 | SimilarContentService（关键词+类型重叠） | WP05 | P1 | 是 |
| T024 | GET /franchises/:id | WP05 | P1 | 是 |
| T025 | GET /tv 列表端点 | WP06 | P2 | 是 |
| T026 | GET /tv/:id 详情 | WP06 | P2 | 是 |
| T027 | GET /tv/:id/seasons/:n | WP06 | P2 | 否 |
| T028 | GET /tv/:id/similar | WP06 | P2 | 是 |
| T029 | GET /anime 列表端点 | WP07 | P2 | 是 |
| T030 | GET /anime/:id 详情 | WP07 | P2 | 是 |
| T031 | GET /anime/:id/seasons/:n | WP07 | P2 | 否 |
| T032 | GET /anime/:id/similar | WP07 | P2 | 是 |
| T033 | GET /people/:id PersonDetail | WP08 | P2 | 是 |
| T034 | 合作者 top-8 查询 | WP08 | P2 | 否 |
| T035 | GET /awards/:slug | WP08 | P2 | 是 |
| T036 | GET /awards/:slug/:edition | WP08 | P2 | 是 |
| T037 | GET /search 全文搜索 + 回退 | WP09 | P2 | 否 |
| T038 | GET /search/autocomplete | WP09 | P2 | 否 |
| T039 | GET /rankings 热门+高分+Top100 | WP09 | P2 | 否 |
| T040 | 电影管理 CRUD | WP10 | P3 | 是 |
| T041 | TV Series 管理 CRUD + 季/集子资源 | WP10 | P3 | 是 |
| T042 | Anime 管理 CRUD | WP10 | P3 | 是 |
| T043 | 影人 + Franchise 管理 CRUD | WP10 | P3 | 是 |
| T044 | GET /admin/stats + 关键词搜索 | WP10 | P3 | 是 |
| T045 | GET /admin/pending 列表 + 详情 | WP11 | P3 | 否 |
| T046 | POST /admin/pending/:id/approve + 预填充 | WP11 | P3 | 否 |
| T047 | POST /admin/pending/:id/reject + /reset | WP11 | P3 | 否 |
| T048 | POST /admin/pending/bulk-approve | WP11 | P3 | 否 |
| T049 | Banner CRUD 端点 | WP11 | P3 | 是 |
| T050 | POST /tracking/view + page_views 插入 | WP12 | P2 | 否 |
| T051 | 每日热度更新后台任务 | WP12 | P2 | 否 |
| T052 | 每日排行榜缓存刷新任务 | WP12 | P2 | 否 |
| T053 | Scrapy 项目配置 | WP13 | P3 | 否 |
| T054 | 代理 + UA 中间件 | WP13 | P3 | 是 |
| T055 | 去重管道 | WP13 | P3 | 是 |
| T056 | PostgreSQL 写入管道 | WP13 | P3 | 否 |
| T057 | TMDB API 蜘蛛 | WP13 | P3 | 是 |
| T058 | 豆瓣 HTML 蜘蛛 | WP13 | P3 | 是 |
| T059 | 时光网 HTML 蜘蛛 | WP13 | P3 | 是 |
| T060 | Vue 3 前端项目配置 | WP14 | P1 | 否 |
| T061 | MediaCard 组件 | WP14 | P1 | 是 |
| T062 | Pagination 组件 | WP14 | P1 | 是 |
| T063 | FilterBar + DropdownFilter 组件 | WP14 | P1 | 是 |
| T064 | Lightbox 组件 | WP14 | P1 | 是 |
| T065 | ImageTabBlock 组件 | WP14 | P1 | 是 |
| T066 | NavBar 组件 | WP15 | P1 | 是 |
| T067 | Footer 组件 | WP15 | P1 | 是 |
| T068 | SearchBar 含自动补全 | WP15 | P1 | 否 |
| T069 | useFilters composable | WP15 | P1 | 否 |
| T070 | useSearch + usePagination composables | WP15 | P1 | 否 |
| T071 | Hero Banner 轮播 | WP16 | P1 | 否 |
| T072 | 热门列表横向滚动区块 | WP16 | P1 | 是 |
| T073 | 热门动漫含国漫/日漫 Tab | WP16 | P1 | 是 |
| T074 | 排行榜 + 奖项入口卡片 | WP16 | P1 | 是 |
| T075 | 电影列表 FilterBar（类型/地区/年代行） | WP17 | P1 | 否 |
| T076 | 语言/评分下拉 + 排序控件 + 网格 | WP17 | P1 | 否 |
| T077 | URL ↔ 过滤器双向同步 | WP17 | P1 | 否 |
| T078 | Pagination + 加载骨架屏 + 空状态 | WP17 | P1 | 否 |
| T079 | 电影详情 Hero 区块 + `<title>` + meta | WP18 | P1 | 否 |
| T080 | 评分块（豆瓣分布条 + IMDB + 时光网） | WP18 | P1 | 是 |
| T081 | 演员网格 + 简介折叠 + 视频 Tab | WP18 | P1 | 是 |
| T082 | 奖项 + Franchise + 相似内容块 | WP18 | P1 | 是 |
| T083 | ImageTabBlock 放置 + 响应式布局 | WP18 | P1 | 否 |
| T084 | TV 列表页含 air_status 过滤器 | WP19 | P2 | 否 |
| T085 | TV 详情 Hero + 下一集块 | WP19 | P2 | 否 |
| T086 | SeasonAccordion 组件 | WP19 | P2 | 否 |
| T087 | TV 季详情页 + 上一季/下一季导航 | WP19 | P2 | 否 |
| T088 | 动漫列表含产地 Tab + 来源类型过滤器 | WP20 | P2 | 否 |
| T089 | 动漫详情页含制作公司/配音演员块 | WP20 | P2 | 否 |
| T090 | 动漫季详情页 | WP20 | P2 | 否 |
| T091 | 影人详情页（个人资料 + 作品 Tab） | WP21 | P2 | 否 |
| T092 | CollaboratorBlock + PhotoWall 组件 | WP21 | P2 | 是 |
| T093 | Franchise 详情页 | WP21 | P2 | 是 |
| T094 | 奖项主页 + 届次详情页 | WP21 | P2 | 是 |
| T095 | 搜索结果页 | WP22 | P2 | 是 |
| T096 | 排行榜页面 | WP22 | P2 | 是 |
| T097 | SEO meta composable + 页面标题 | WP22 | P2 | 否 |
| T098 | 图片懒加载 + 关键图预加载 | WP22 | P2 | 是 |
| T099 | 管理项目配置 + OAuth PKCE 登录 | WP23 | P3 | 否 |
| T100 | 管理布局 + 仪表盘统计页 | WP23 | P3 | 否 |
| T101 | 电影管理列表页 | WP23 | P3 | 否 |
| T102 | 电影管理创建/编辑表单 | WP23 | P3 | 否 |
| T103 | TV Series 管理 CRUD 页面 | WP24 | P3 | 是 |
| T104 | Anime 管理 CRUD 页面 | WP24 | P3 | 是 |
| T105 | 影人管理 CRUD 页面 | WP24 | P3 | 是 |
| T106 | Franchise 管理 CRUD 页面 | WP24 | P3 | 是 |
| T107 | 爬虫审核列表 | WP25 | P3 | 否 |
| T108 | 审核详情 + 通过流程 | WP25 | P3 | 否 |
| T109 | 拒绝 + 重置工作流 | WP25 | P3 | 否 |
| T110 | Banner 管理页面 | WP25 | P3 | 是 |
| T111 | Sentry 集成（后端 + 前端） | WP26 | P2 | 是 |
| T112 | Prometheus 指标端点 | WP26 | P2 | 是 |
| T113 | Nginx 配置 | WP26 | P2 | 否 |
| T114 | Docker Compose 本地开发配置 | WP26 | P2 | 否 |
| T115 | CI/CD 流水线存根 | WP26 | P2 | 否 |
| T116 | xUnit 项目配置 + Testcontainers + 覆盖率配置 | WP27 | P2 | 否 |
| T117 | 领域实体单元测试（软删除、通过/拒绝、Banner 激活） | WP27 | P2 | 是 |
| T118 | 应用服务单元测试（缓存命中/未命中、批量通过、搜索回退） | WP27 | P2 | 是 |
| T119 | 仓储集成测试（数组过滤器、软删除可见性、Redis） | WP27 | P2 | 是 |
| T120 | API 控制器集成测试（401 守卫、软删除往返） | WP27 | P2 | 否 |
| T121 | Vitest + @vue/test-utils 配置（jsdom、路由存根、coverage v8） | WP28 | P2 | 否 |
| T122 | MediaCard + ImageTabBlock 测试 | WP28 | P2 | 是 |
| T123 | FilterBar + Pagination 测试 | WP28 | P2 | 是 |
| T124 | SearchBar 自动补全测试（防抖、下拉框、键盘） | WP28 | P2 | 否 |
| T125 | BannerCarousel + SynopsisBlock 测试（计时器、空状态、折叠） | WP28 | P2 | 否 |
| T126 | 完整演职员页面 /movies/:id/credits | WP18 | P1 | 否 |
| T127 | pytest 配置 + HTML fixtures + conftest.py（fake_response、mock_db） | WP29 | P2 | 否 |
| T128 | 蜘蛛提取测试（电影标题/评分/类型，TV air_status，动漫 origin） | WP29 | P2 | 是 |
| T129 | 管道单元测试（DeduplicationPipeline + PostgresPipeline） | WP29 | P2 | 是 |
| T130 | 设置 + 中间件测试（DOWNLOAD_DELAY ≥3 回归守卫、ROBOTSTXT_OBEY、UA 轮换） | WP29 | P2 | 否 |
| T131 | GET /tv/:id/credits 完整演职员 API | WP06 | P2 | 否 |
| T132 | GET /anime/:id/credits 完整演职员 API | WP07 | P2 | 否 |
| T133 | TV 完整演职员页面 /tv/[id]/credits | WP19 | P2 | 否 |
| T134 | 动漫完整演职员页面 /anime/[id]/credits | WP20 | P2 | 否 |
| T135 | TV 详情图片 Tab 区块接入 | WP19 | P2 | 否 |
| T136 | TV 详情相似内容区块接入 | WP19 | P2 | 否 |
| T137 | 奖项基础数据初始化（7 个奖项事件） | WP08 | P2 | 否 |
| T138 | 奖项最小可展示数据集（每奖项至少 1 届） | WP08 | P2 | 否 |
| T139 | 跨浏览器冒烟验收（Chrome/Edge/Safari iOS） | WP26 | P2 | 否 |
| T140 | 管理审核流程效率验收 | WP26 | P2 | 否 |
| T141 | 搜索相关性验收（FTS + 前缀回退） | WP26 | P2 | 否 |
| T142 | SLA/SLO 验收口径与告警回滚阈值 | WP26 | P2 | 否 |
| T143 | 文档与注释合规门禁 | WP26 | P2 | 否 |
| T144 | SEO 验收自动化 | WP26 | P2 | 否 |
| T145 | 性能基线验收 | WP26 | P2 | 否 |
| T146 | 缓存与发布验证 | WP26 | P2 | 否 |
| T147 | 并发容量验收（5000 并发阶梯压测） | WP26 | P2 | 否 |

---

> 本 tasks.md 由 `/spec-kitty.tasks` 生成，并经 `/spec-kitty.analyze` 整改更新。共 29 个工作包，147 个子任务。
