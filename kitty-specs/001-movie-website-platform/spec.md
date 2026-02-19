# 影视资讯网站平台

## Overview

### Problem Statement

中国大陆用户缺乏一个结构清晰、内容完整的影视资讯平台，现有平台（豆瓣、时光网等）或功能分散、或社交噪音过多。本项目旨在构建一个纯内容展示类影视资讯网站，涵盖电影、电视剧、动漫、影人四大板块，提供高质量的内容浏览、筛选、搜索与资讯查阅体验。

### Goals

- 为用户提供结构清晰的影视内容目录，涵盖电影、电视剧、动漫三大品类
- 提供准确完整的影视详情页（简介、演职员、上映信息、预告片等）
- 支持多维度浏览与筛选（类型、年份、地区、评分）
- 提供全站搜索与热门/高分排行榜
- 支持管理员在后台手动增删改所有影视与影人内容
- 爬虫数据经后台审核后方可对外展示，保障内容质量

### Non-Goals

- 用户注册、登录、个人中心功能
- 用户评分、短评、长评、点赞等社交互动功能
- 用户收藏夹、想看/在看/看过列表
- 在线视频播放、弹幕功能
- 付费会员或广告变现体系
- 移动端原生 App（iOS / Android）
- 多语言国际化（本期仅中文）
- 票房数据、上座率实时展示

---

## Actors

| Actor | Description |
|-------|-------------|
| 访客（Visitor） | 未登录的普通用户，可浏览所有公开内容 |
| 内容管理员（Admin） | 经 OAuth 2.0 鉴权的后台管理员，可增删改所有内容 |
| 爬虫系统（Crawler） | 自动化数据采集脚本，将数据写入待审核暂存区 |

---

## User Scenarios & Testing

### 场景 1：访客浏览首页

**前置条件**：数据库中已有已发布的影视内容

**操作步骤**：
1. 访客打开网站首页
2. 看到 Hero Banner 轮播区（至少 3 条精选内容，自动轮播）
3. 看到「近期热门电影」横向滚动卡片列表（至少 8 条）
4. 看到「近期热门电视剧」横向滚动卡片列表（至少 8 条）
5. 看到「近期热门动漫」模块，含「国漫」/「日漫」两个 Tab（各至少 8 条）

**预期结果**：
- 每个卡片显示封面图（2:3 比例）、标题、年份、评分
- 页面在 1280px 宽度下无横向滚动条

---

### 场景 2：访客筛选电影列表

**前置条件**：数据库中已有多部不同类型、地区、年份的电影

**操作步骤**：
1. 访客进入 `/movies` 页面
2. 选择类型「科幻」、地区「欧美」、年份「2023」
3. 选择排序方式「综合评分」
4. 翻到第 2 页

**预期结果**：
- URL 中包含对应 query params（如 `?genre=sci-fi&region=us&year=2023&sort=score&page=2`）
- 页面展示符合条件的电影，每页最多 24 条
- 分页组件正常工作

---

### 场景 3：访客查看电影详情

**前置条件**：数据库中存在 ID 为 `123` 的电影，含完整信息

**操作步骤**：
1. 访客进入 `/movies/123`
2. 查看基础信息区域
3. 点击演员头像
4. 点击「展开」查看完整剧情简介

**预期结果**：
- 页面顶部展示横幅背景图（模糊处理）+ 海报 + 基础信息
- 基础信息包含：中英文标题、豆瓣评分、IMDB 评分、上映日期、片长、类型标签、导演、主演
- 点击演员头像跳转 `/people/[id]`
- 剧情简介超过 150 字时默认折叠，点击「展开」显示全文
- `<title>` 格式为：`{影片名} ({年份}) - 影视网`

---

### 场景 4：访客搜索内容

**前置条件**：数据库中有多种类型的内容

**操作步骤**：
1. 访客点击顶部导航搜索图标
2. 输入关键词「星际穿越」并按回车
3. 在搜索结果页切换到「电影」Tab

**预期结果**：
- 跳转至 `/search?q=星际穿越`
- 结果分 Tab 显示：全部 / 电影 / 电视剧 / 动漫 / 影人
- 每条结果显示封面缩略图、标题、年份、类型
- 无结果时显示「未找到与「{keyword}」相关的内容」

---

### 场景 5：管理员审核爬虫内容

**前置条件**：爬虫已抓取数据写入 `pending_content` 暂存区

**操作步骤**：
1. 管理员登录后台 `/admin`
2. 进入「爬虫审核」列表
3. 点击某条目查看详情
4. 点击「通过」按钮

**预期结果**：
- 该条目状态更新为 `approved`，内容写入正式表，前台可见
- 支持批量勾选后执行「批量通过」

---

### 场景 6：管理员手动新增电影

**前置条件**：管理员已登录后台

**操作步骤**：
1. 管理员进入 `/admin/content/new`，选择「电影」
2. 填写中文标题、简介、类型标签、上映日期、海报图
3. 点击「提交」

**预期结果**：
- 内容直接写入正式表（状态 `published`），无需审核
- 跳转至该条目编辑页，显示「创建成功」提示
- 必填字段为空时提交按钮不可用，显示行内错误提示

---

## Functional Requirements

### 内容展示

**FR-1**：系统支持三种内容类型实体：Movie、TVSeries、Anime，各有独立路由前缀（`/movies`、`/tv`、`/anime`）

**FR-2**：每种内容类型有：列表页（含筛选/排序/分页）、详情页、全部演职员页

**FR-3**：影人（Person）有独立详情页 `/people/[id]`，展示基础档案、获奖记录、合作影人与关联作品列表

**FR-4**：详情页演职员头像/姓名可点击，跳转对应影人详情页

**FR-5**：所有列表页筛选与排序条件须体现在 URL query params 中，保证链接可分享

**FR-6**：列表页默认每页 24 条，支持分页导航（上一页 / 下一页 / 页码跳转）

**FR-7**：所有图片须有语义化 `alt` 文本；图片加载失败显示灰色占位符

**FR-8**：电影支持归属系列（Franchise），系列有独立页面 `/franchises/[id]`

**FR-9**：电影支持奖项记录（Award），含奖项名称、届次、类别、获奖人、获奖/提名状态

**FR-10**：电视剧与动漫支持季（Season）与集（Episode）两级层级结构，季有独立详情页

**FR-11**：电影评分存储豆瓣评分（含5星分布）、IMDB 评分、时光网分项评分（音乐/画面/导演/故事/表演）

**FR-12**：电影上映日期支持多地区多日期记录（含点映/公映类型标注）

**FR-13**：电影片长支持多版本记录（如北美版/欧洲版）

**FR-14**：奖项有独立专题页（`/awards/[slug]/[edition]`），支持历届导航，至少覆盖奥斯卡、金球奖、戛纳、威尼斯、柏林、金像奖、金马奖

**FR-15**：动漫实体包含 `origin` 字段（`cn` 国漫 / `jp` 日漫 / `other`），列表页与首页均按此字段区分展示

**FR-16**：影人页展示「合作过的影人」模块（合作次数最多的前 8 人）

**FR-17**：影人作品列表支持按职务 Tab 过滤（全部 / 导演 / 编剧 / 演员）

### 搜索

**FR-18**：全站搜索覆盖 Movie、TVSeries、Anime、Person 四类实体

**FR-19**：搜索结果按相关度排序，支持按类型 Tab 过滤

### 排行榜

**FR-20**：提供热门榜（按近 7 日访问量）与高分榜（按评分），每日定时更新

**FR-21**：排行榜支持分类型榜单（按类型 + 评分筛选），每个榜单展示前 50 条

### 数据管理

**FR-22**：爬虫数据先存入 `pending_content` 暂存区，管理员审核通过后方可前台展示

**FR-23**：后台 `/admin` 路由须有 OAuth 2.0 身份验证保护

**FR-24**：管理员可在后台直接新增 Movie / TVSeries / Anime / Person / Franchise，提交后状态为 `published`，无需审核

**FR-25**：管理员可编辑任意已发布条目的所有字段，包括演职员关联、奖项、季/集信息

**FR-26**：删除操作采用软删除（`deleted_at` 字段），前台不展示，后台可恢复

**FR-27**：后台内容列表支持按标题关键词搜索，并提供各内容类型的数量统计概览

**FR-28**：爬虫请求频率须可配置，默认间隔 ≥ 3 秒，支持随机 User-Agent 池与 HTTP 代理列表配置

### SEO 与性能

**FR-29**：每个详情页须有独立语义化的 `<title>` 与 `<meta name="description">`

**FR-30**：图片列表中使用懒加载；首屏关键图片使用预加载

---

## Key Entities

### Movie（电影）

| 字段 | 说明 |
|------|------|
| id | 主键 |
| title_cn | 中文标题 |
| title_original | 原文标题 |
| title_aliases | 别名数组 |
| synopsis | 剧情简介 |
| genres | 类型标签数组 |
| region | 制片地区数组 |
| language | 语言数组 |
| release_dates | 多地区上映日期（JSONB） |
| durations | 多版本片长（JSONB） |
| douban_score | 豆瓣评分 |
| douban_rating_count | 豆瓣评分人数 |
| douban_rating_dist | 豆瓣5星分布（JSONB） |
| imdb_score | IMDB 评分 |
| imdb_id | IMDB 编号 |
| mtime_score_* | 时光网分项评分（音乐/画面/导演/故事/表演） |
| poster_cos_key | 海报 COS 路径 |
| backdrop_cos_key | 横幅 COS 路径 |
| trailer_url | 预告片链接 |
| production_companies | 出品公司数组 |
| distributors | 发行公司数组 |
| franchise_id | 所属系列 ID |
| status | published / draft |
| deleted_at | 软删除时间戳 |

### TVSeries / Anime（电视剧 / 动漫）

在 Movie 基础字段上额外包含：
- `air_status`：连载中 / 已完结
- `first_air_date`：首播日期
- `production_companies`：出品公司
- Anime 额外：`origin`（cn/jp/other）、`studio`（制作公司）、`source_material`（原作来源）

### TVSeason / AnimeSeason（季）

| 字段 | 说明 |
|------|------|
| id | 主键 |
| series_id / anime_id | 所属剧集/动漫 ID |
| season_number | 季序号 |
| name | 季名 |
| episode_count | 集数 |
| first_air_date | 首播年份 |
| poster_cos_key | 季海报 |
| overview | 季简介 |

### Person（影人）

| 字段 | 说明 |
|------|------|
| id | 主键 |
| name_cn | 中文姓名 |
| name_en | 英文姓名 |
| name_aliases | 别名数组 |
| gender | 性别 |
| birth_date | 出生日期 |
| death_date | 去世日期（可空） |
| birth_place | 出生地 |
| nationality | 国籍 |
| professions | 职业标签数组 |
| biography | 个人简介 |
| imdb_id | IMDB 编号 |
| family_members | 家庭成员（JSONB） |
| avatar_cos_key | 头像 COS 路径 |

### Credit（演职员关联）

| 字段 | 说明 |
|------|------|
| id | 主键 |
| person_id | 影人 ID |
| content_type | movie / tv / anime |
| content_id | 内容 ID |
| role | director / writer / actor / producer / ... |
| character_name | 角色名 |
| display_order | 显示顺序 |

### AwardEvent / AwardCeremony / AwardNomination（奖项三级结构）

- **AwardEvent**：奖项主表（奥斯卡、金球奖等），含 slug 字段
- **AwardCeremony**：届次表，关联 AwardEvent
- **AwardNomination**：提名/获奖记录，关联 Ceremony、Movie、Person

### PendingContent（爬虫暂存区）

| 字段 | 说明 |
|------|------|
| id | 主键 |
| source | 数据来源（douban / mtime / tmdb） |
| source_url | 原始 URL |
| content_type | movie / tv / anime |
| raw_data | 爬取原始数据（JSONB） |
| review_status | pending / approved / rejected |
| reviewed_at | 审核时间 |

---

## Success Criteria

1. **内容覆盖**：Phase 1 上线时电影库存量 ≥ 500 部，含海报与简介
2. **SEO 达标**：所有详情页 Lighthouse SEO 评分 ≥ 90
3. **性能达标**：首页 LCP ≤ 2.5s（移动端 4G），搜索结果响应 P95 ≤ 500ms，列表筛选切换 ≤ 300ms
4. **跨浏览器兼容**：所有页面在 Chrome / Firefox / Safari 最新版本无布局错误
5. **响应式布局**：手机（< 768px）、平板（768–1024px）、桌面（> 1024px）均无错位
6. **内容质量**：爬虫数据 100% 经审核后方可前台展示，无未审核内容直接上线
7. **管理效率**：管理员可在 3 步内完成单条内容的新增或编辑操作
8. **搜索可用性**：搜索结果页在有数据时，相关内容出现在首屏前 5 条内

---

## Assumptions

1. 展示前端若 SEO 评分要求严格，将采用 Nuxt 3（基于 Vue 3 的 SSR/SSG 框架）替代纯 Vite SPA
2. 搜索使用 PostgreSQL 全文搜索（`tsvector` + `tsquery` + GIN 索引），无需引入 Elasticsearch
3. 图片存储使用腾讯云 COS，数据库只存储 COS object key，前台拼接 CDN 域名访问
4. 爬虫优先使用 TMDB 官方 API，豆瓣与时光网使用 HTML 解析，遵守各站 robots.txt
5. 管理后台鉴权采用 OAuth 2.0 Authorization Code + PKCE 模式，JWT Access Token 有效期 2 小时
6. 排行榜数据每日凌晨 2 点通过定时任务更新，缓存 TTL 24 小时
7. 暂不考虑种子数据预置，Phase 1 数据通过爬虫 + 人工录入方式准备

---

## Dependencies

- 腾讯云 COS 账号与 CDN 域名配置
- TMDB API Key（爬虫使用）
- PostgreSQL 15+ 数据库（需安装 `zhparser` 扩展支持中文全文搜索）
- Redis 实例（缓存层）

---

## Phased Delivery

| 阶段 | 内容 | 包含功能 |
|------|------|---------|
| Phase 1 | 核心内容展示上线 | 首页、导航、电影列表页、电影详情页、影人详情页 |
| Phase 2 | 完整品类 + 搜索 + 排行 | 电视剧/动漫列表与详情、搜索、排行榜、系列页、奖项页 |
| Phase 3 | 数据采集与后台管理 | 爬虫系统、后台审核、后台内容管理（增删改）、影人管理 |
