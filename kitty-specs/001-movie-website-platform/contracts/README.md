# API Contracts: 影视资讯网站平台

**Feature**: 001-movie-website-platform
**Date**: 2026-02-21
**Style**: RESTful JSON API
**Auth**: OAuth 2.0 JWT RS256（仅 `/api/admin/**` 路由需要）

---

## 基础约定

- **Base URL**: `/api/v1`
- **分页**: 所有列表接口均支持 `page`（默认 1）+ `page_size`（默认 24，最大 100）
- **软删除**: 所有接口默认过滤 `deleted_at IS NOT NULL` 的记录
- **响应格式**:

```json
{
  "data": { ... },
  "pagination": { "page": 1, "page_size": 24, "total": 120, "total_pages": 5 }
}
```

- **错误格式**:

```json
{ "error": { "code": "NOT_FOUND", "message": "内容不存在" } }
```

---

## 合约文件索引

| 文件 | 覆盖接口 |
|------|---------|
| [movies.yaml](./movies.yaml) | 电影列表、详情、演职员 |
| [tv.yaml](./tv.yaml) | 电视剧列表、详情、季/集详情 |
| [anime.yaml](./anime.yaml) | 动漫列表、详情、季/集详情 |
| [people.yaml](./people.yaml) | 影人详情、合作影人 |
| [search.yaml](./search.yaml) | 全站搜索、自动补全 |
| [rankings.yaml](./rankings.yaml) | 热门榜、高分榜 |
| [awards.yaml](./awards.yaml) | 奖项主页、届次详情 |
| [admin.yaml](./admin.yaml) | 后台 CRUD、爬虫审核、Banner 管理 |

---

## 快速参考：所有端点

### 公开接口

```
GET  /api/v1/home                               # 首页数据（Banner + 各分类热门）
GET  /api/v1/movies                             # 电影列表（含筛选/排序/分页）
GET  /api/v1/movies/:id                         # 电影详情
GET  /api/v1/movies/:id/credits                 # 全部演职员
GET  /api/v1/movies/:id/similar                 # 相似电影（6条）
GET  /api/v1/tv                                 # 电视剧列表
GET  /api/v1/tv/:id                             # 电视剧详情
GET  /api/v1/tv/:id/seasons/:season_number      # 季详情（含集列表）
GET  /api/v1/tv/:id/credits                     # 全部演职员
GET  /api/v1/tv/:id/similar                     # 相似电视剧
GET  /api/v1/anime                              # 动漫列表
GET  /api/v1/anime/:id                          # 动漫详情
GET  /api/v1/anime/:id/seasons/:season_number   # 季详情（含集列表）
GET  /api/v1/anime/:id/credits                  # 全部演职员
GET  /api/v1/anime/:id/similar                  # 相似动漫
GET  /api/v1/people/:id                         # 影人详情
GET  /api/v1/franchises/:id                     # 系列详情（含电影列表）
GET  /api/v1/awards/:slug                       # 奖项主页
GET  /api/v1/awards/:slug/:edition              # 届次详情
GET  /api/v1/search?q=                          # 全站搜索结果
GET  /api/v1/search/autocomplete?q=             # 实时自动补全
GET  /api/v1/rankings                           # 排行榜（含 movie/tv/anime 三类）
```

### 管理接口（需 JWT Bearer Token）

```
# 内容管理
POST   /api/v1/admin/movies                     # 新增电影
PUT    /api/v1/admin/movies/:id                 # 编辑电影
DELETE /api/v1/admin/movies/:id                 # 软删除电影
# tv / anime / people / franchises 同上结构

# 爬虫审核
GET    /api/v1/admin/pending                    # 待审核列表
GET    /api/v1/admin/pending/:id                # 待审核详情
POST   /api/v1/admin/pending/:id/approve        # 通过（返回预填充表单数据）
POST   /api/v1/admin/pending/:id/reject         # 拒绝
POST   /api/v1/admin/pending/:id/reset          # rejected → pending
POST   /api/v1/admin/pending/bulk-approve       # 批量通过

# Hero Banner
GET    /api/v1/admin/banners                    # Banner 列表
POST   /api/v1/admin/banners                    # 新增 Banner 条目
PUT    /api/v1/admin/banners/:id                # 编辑顺序/时间段
DELETE /api/v1/admin/banners/:id                # 删除 Banner 条目

# 统计概览
GET    /api/v1/admin/stats                      # 各类型内容数量统计
```

---

## 核心 Schema 片段

### MediaCardDto（列表卡片通用）
```json
{
  "id": 123,
  "content_type": "movie",
  "title_cn": "星际穿越",
  "year": 2014,
  "poster_cos_key": "posters/xxx.jpg",
  "douban_score": 9.3,
  "genres": ["科幻", "冒险"]
}
```

### 电影详情 Response 关键字段
```json
{
  "id": 123,
  "title_cn": "复仇者联盟2",
  "title_original": "Avengers: Age of Ultron",
  "title_aliases": ["复联2"],
  "synopsis": "...",
  "genres": ["动作", "科幻"],
  "region": ["美国"],
  "language": ["英语"],
  "release_dates": [
    {"region": "中国大陆", "date": "2015-05-12", "type": "正式公映"}
  ],
  "durations": [{"version": "院线版", "minutes": 141}],
  "douban_score": 7.8,
  "douban_rating_count": 1200000,
  "douban_rating_dist": {"five": 18.2, "four": 35.6, "three": 30.1, "two": 11.5, "one": 4.6},
  "imdb_score": 7.3,
  "imdb_id": "tt2395427",
  "mtime_scores": {"music": 7.2, "visual": 8.9, "director": 7.5, "story": 7.0, "performance": 7.8},
  "poster_cos_key": "posters/xxx.jpg",
  "backdrop_cos_key": "backdrops/xxx.jpg",
  "extra_posters": ["posters/p1.jpg", "posters/p2.jpg"],
  "extra_backdrops": ["backdrops/b1.jpg"],
  "franchise": {"id": 5, "name_cn": "复仇者联盟系列", "order": 2, "total": 4},
  "cast": [
    {"person_id": 88, "name_cn": "小罗伯特·唐尼", "avatar_cos_key": "...", "character_name": "钢铁侠", "display_order": 1}
  ],
  "directors": [{"person_id": 55, "name_cn": "乔斯·韦登"}],
  "awards": [
    {"event_cn": "土星奖", "edition": 42, "category": "最佳科幻影片", "is_winner": true}
  ],
  "videos": [
    {"id": 1, "title": "官方预告", "url": "https://...", "type": "trailer"}
  ],
  "similar": [...]
}
```

### 搜索自动补全 Response
```json
{
  "data": {
    "movies": [
      {"id": 1, "title_cn": "星际穿越", "year": 2014, "poster_cos_key": "..."}
    ],
    "tv_series": [...],
    "anime": [...],
    "people": [
      {"id": 88, "name_cn": "克里斯托弗·诺兰", "avatar_cos_key": "..."}
    ],
    "see_all_url": "/search?q=星际"
  }
}
```
