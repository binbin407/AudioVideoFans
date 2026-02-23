# Research: 影视资讯网站平台

**Feature**: 001-movie-website-platform
**Date**: 2026-02-21

---

## 研究议题总览

| # | 议题 | 状态 | 结论 |
|---|------|------|------|
| R1 | SqlSugar + PostgreSQL DDD 仓储模式 | ✅ 已解决 | 见下 |
| R2 | PostgreSQL zhparser 中文全文搜索配置 | ✅ 已解决 | 见下 |
| R3 | Vue 3 Vite SPA SEO 优化策略 | ✅ 已解决 | 见下 |

---

## R1：SqlSugar + PostgreSQL DDD 仓储模式

### 决策

- **IRepository 接口置于 Domain 层**，不依赖任何 ORM 具体类型
- **SqlSugar 实现置于 Infrastructure 层**，通过 DI 注入
- **Scoped 生命周期**：`ISqlSugarClient` 必须注册为 Scoped（per-request），Singleton 会破坏跨仓储事务

### DI 配置（Program.cs）

```csharp
services.AddScoped<ISqlSugarClient>(sp =>
    new SqlSugarClient(new ConnectionConfig
    {
        DbType                = DbType.PostgreSQL,
        ConnectionString      = configuration.GetConnectionString("Default")!,
        IsAutoCloseConnection = true,
        InitKeyType           = InitKeyType.Attribute,
        MoreSettings = new ConnMoreSettings
        {
            PgSqlIsAutoToLower    = false,  // ⚠️ REQUIRED: 否则与 ColumnName 大小写冲突
            IsAutoRemoveDataCache = true,
        }
    })
);
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IMovieRepository, MovieRepository>();
// ... 其他 Repository
```

### 实体属性标注规范

```csharp
[SugarTable("movies")]
public class Movie
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "title_cn")]
    public string TitleCn { get; set; } = string.Empty;

    // JSONB 列（release_dates, douban_rating_dist 等）
    [SugarColumn(ColumnName = "release_dates", ColumnDataType = "jsonb", IsJson = true)]
    public List<ReleaseDate> ReleaseDates { get; set; } = new();

    // TEXT[] 数组列
    [SugarColumn(ColumnName = "genres", ColumnDataType = "text[]")]
    public string[] Genres { get; set; } = Array.Empty<string>();

    // Generated Column（search_vector）—— 数据库计算，ORM 忽略写入
    [SugarColumn(ColumnName = "search_vector", IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true)]
    public string? SearchVector { get; set; }
}
```

### TEXT[] 数组过滤（必须用原生 SQL）

SqlSugar LINQ 无法翻译 PostgreSQL 数组运算符 `&&`（overlap）：

```csharp
// ✅ 安全参数化原生 SQL
var results = await _db.Ado.SqlQueryAsync<Movie>(
    "SELECT * FROM movies WHERE genres && @genres::text[] AND deleted_at IS NULL",
    new { genres = filter.Genres }
);
```

### Unit of Work 模式

因所有 Repository 共享同一 Scoped `ISqlSugarClient`，调用 `BeginTran()` 后所有后续仓储操作自动归入同一事务：

```csharp
// Application Layer（用例边界）
await _uow.BeginAsync(ct);
try
{
    await _movieRepo.AddAsync(movie, ct);
    await _pendingRepo.UpdateStatusAsync(pendingId, "approved", ct);
    await _uow.CommitAsync(ct);
}
catch { await _uow.RollbackAsync(ct); throw; }
```

### 已知陷阱

| 问题 | 解决方案 |
|------|---------|
| `PgSqlIsAutoToLower` 默认 true | 设为 false，全部依赖显式 `[SugarColumn(ColumnName = "...")]` |
| 无全局软删除过滤 | 每个仓储读方法加 `.Where(x => x.DeletedAt == null)` |
| LINQ 无法翻译 `&&` / `ANY` / `@>` | 用 `.Where(rawSql)` 或 `Db.Ado.SqlQueryAsync` + 命名参数 |
| JSONB 可空列 | C# 类型用 `T?`，否则 NULL 反序列化报错 |
| GIN 索引未自动创建 | 所有 `text[]` 和 `jsonb` 过滤列须手动写迁移 SQL |

---

## R2：PostgreSQL zhparser 中文全文搜索

### 决策

- **搜索向量实现方式**：使用 `GENERATED ALWAYS AS ... STORED` 生成列，而非触发器
  - 理由：内容更新频率低（管理员驱动），数据库自动维护，无需 ORM 感知，无触发器丢失风险
- **文本搜索配置名**：`chinese_zh`（Parser: zhparser）
- **权重方案**：标题 A → 原文标题/别名 B → 简介/传记 C
- **降级策略**：应用层启动时检查 `pg_extension`，zhparser 不可用时切换到 pg_trgm + ILIKE

### 安装与配置 SQL

```sql
-- 1. 安装扩展
CREATE EXTENSION IF NOT EXISTS zhparser;
CREATE EXTENSION IF NOT EXISTS pg_trgm;   -- ILIKE 降级用

-- 2. 创建中文搜索配置
CREATE TEXT SEARCH CONFIGURATION chinese_zh (PARSER = zhparser);
ALTER TEXT SEARCH CONFIGURATION chinese_zh
    ADD MAPPING FOR n, v, a, i, e, l, j, h, k, x WITH simple;

-- 3. zhparser 调优（postgresql.conf 或 ALTER SYSTEM）
-- zhparser.multi_short = on   -- 输出短词，支持单字查询（如「李」）
-- zhparser.seg_with_duality = off
```

### Generated Column 定义（movies 示例）

```sql
ALTER TABLE movies
    ADD COLUMN search_vector tsvector
        GENERATED ALWAYS AS (
            setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')),       'A') ||
            setweight(to_tsvector('simple',     coalesce(title_original, '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(
                array_to_string(title_aliases, ' '), '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')),       'C')
        ) STORED;
-- people 表：title_cn→name_cn(A), title_original→name_en 用 simple(A), synopsis→biography(C)
```

### GIN 索引（部分索引，排除软删除行）

```sql
CREATE INDEX CONCURRENTLY idx_movies_search_vector
    ON movies USING GIN (search_vector) WHERE deleted_at IS NULL;
-- tv_series / anime / people 同上
```

### 核心搜索查询模式

```sql
-- 用户输入搜索（最安全，AND 语义）
plainto_tsquery('chinese_zh', $1)

-- 高级搜索（支持 -word 排除、"短语"、OR）
websearch_to_tsquery('chinese_zh', $1)

-- 搜索结果排序
ts_rank_cd(search_vector, query)   -- 覆盖密度排名，适合长文本
```

### 自动补全（每类最多 3 条）

```sql
WITH q AS (SELECT plainto_tsquery('chinese_zh', $1) AS tsq),
ranked_movies AS (
    SELECT id, title_cn, poster_cos_key, 'movie' AS type,
           ROW_NUMBER() OVER (ORDER BY ts_rank(search_vector, tsq) DESC) AS rn
    FROM movies, q WHERE search_vector @@ tsq AND deleted_at IS NULL
),
-- tv / anime / people 同上 ...
SELECT id, title_cn, poster_cos_key, type
FROM (
    SELECT * FROM ranked_movies WHERE rn <= 3 UNION ALL ...
) combined;
```

### 降级优先级

```
1. zhparser FTS + GIN index       — 最佳中文相关度（主路径）
2. pg_trgm trigram + GIN index    — 无分词，但索引支持（中间降级）
3. 纯 ILIKE（无索引）              — 最后兜底，O(N) 扫描
```

应用层在启动时查询 `pg_extension` 缓存结果，路由到对应查询分支。

---

## R3：Vue 3 Vite SPA SEO 优化

### 背景

用户已接受「放弃 Nuxt 3 SSR，使用 Vue 3 + Vite SPA」（planning Q1），SEO ≥ 90 目标降级。以下为 SPA 环境下可实施的最大化 SEO 优化方案。

### 决策

| 方案 | 实施成本 | SEO 效果 | 选择 |
|------|---------|---------|------|
| vite-plugin-ssr / vite-ssg（静态预渲染） | 中 | 高（静态页面可爬取） | **推荐用于详情页** |
| vue-meta / @unhead/vue（动态 meta） | 低 | 中（SPA 动态 meta，爬虫可能不执行 JS） | 必做（兜底） |
| 纯 SPA 无预渲染 | 无 | 低 | 不可接受 |

**最终策略**：使用 `vite-plugin-ssg`（Vue 官方推荐的静态站点生成插件）对**已知路由**预渲染静态 HTML。详情页通过 `generateRoutes` 动态生成（需 API 提供 ID 列表）。

### 实施要点

```bash
npm install vite-plugin-ssg @unhead/vue
```

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import generateSitemap from 'vite-ssg-sitemap'

export default defineConfig({
  plugins: [vue()],
  ssgOptions: {
    script: 'async',
    formatting: 'minify',
    onFinished() { generateSitemap() }
  }
})
```

```typescript
// main.ts — 使用 ViteSSG 替代 createApp
import { ViteSSG } from 'vite-ssg'
import App from './App.vue'
import { routes } from './router'

export const createApp = ViteSSG(App, { routes })
```

### Meta 标签规范（@unhead/vue）

```typescript
// 所有详情页
useHead({
  title: `${movie.titleCn} (${movie.year}) - 影视网`,
  meta: [
    { name: 'description', content: movie.synopsis?.slice(0, 150) ?? '' },
    { property: 'og:title', content: movie.titleCn },
    { property: 'og:image', content: cosUrl(movie.posterCosKey) },
    { property: 'og:type', content: 'video.movie' },
  ]
})
```

### Sitemap 生成

`vite-ssg-sitemap` 在构建时自动从路由生成 `/sitemap.xml`，提交至百度搜索资源平台与 Google Search Console。

---

## 附录：关键设计决策汇总

| 决策 | 选择 | 备选方案 | 放弃原因 |
|------|------|---------|---------|
| 搜索向量维护 | Generated Column（STORED） | 触发器 | 触发器可能被意外禁用，Generated Column 更安全 |
| 搜索配置名 | `chinese_zh` | `chinese` | `chinese_zh` 更明确，避免与系统配置冲突 |
| ILIKE 降级中间层 | pg_trgm GIN | 纯 ILIKE | pg_trgm 支持索引，O(log N) vs O(N) |
| SqlSugar 生命周期 | Scoped | Singleton / Transient | Singleton 破坏跨仓储事务；Transient 每个 Repo 独立连接 |
| 展示前端 SEO | vite-plugin-ssg 预渲染 | 纯 SPA | 纯 SPA 爬虫无法执行 JS，SEO 为 0 |
