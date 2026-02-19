# PRD: 影视资讯网站

## Introduction

构建一个面向中国大陆用户的影视资讯网站，涵盖**电影、电视剧、动漫、影人**四大板块。参考 TMDB、豆瓣电影、时光网等平台的内容组织方式，提供高质量的内容浏览、筛选、搜索与资讯查阅体验。

本网站定位为**纯内容展示类**，不含用户注册、评论等社交功能。数据通过**爬虫抓取 + 人工审核**方式维护，同时支持管理员在后台手动增删改所有影视与影人内容，分三个阶段迭代上线。

---

## Goals

- 提供结构清晰的影视内容目录，涵盖电影、电视剧、动漫三大品类
- 为用户提供准确完整的影视详情页（简介、演职员、上映信息、预告片等）
- 电影支持系列（franchise）归组、奖项记录与独立排行榜
- 电视剧与动漫支持季（Season）与集（Episode）的层级结构
- 提供影人基础档案页（个人简介 + 参演/执导作品列表）
- 支持按类型、年份、地区、评分等多维度浏览与筛选
- 提供全站搜索，快速定位影片或影人
- 展示热门排行榜与高分榜
- 爬虫数据经后台审核后方可对外展示，保障内容质量
- 管理员可在后台手动新增、编辑、删除所有影视内容与影人档案
- 分三阶段上线：Phase 1（核心展示）→ Phase 2（搜索+排行）→ Phase 3（爬虫+后台管理）

---

## User Stories

### US-001: 首页展示
**Description:** 作为访客，我希望在首页看到精选推荐和近期热门内容，以便快速发现感兴趣的影视作品。

**Acceptance Criteria:**
- [ ] 首页顶部有 Hero Banner 轮播区（精选内容，至少 3 条，自动轮播）
- [ ] 展示「近期热门电影」横向滚动卡片列表（至少 8 条）
- [ ] 展示「近期热门电视剧」横向滚动卡片列表（至少 8 条）
- [ ] 展示「近期热门动漫」横向滚动卡片列表，分「国漫」/「日漫」两个 Tab（各至少 8 条）
- [ ] 每个卡片显示：封面图（2:3 比例）、标题、年份、评分（如有）
- [ ] 页面在 1280px 宽度下无横向滚动条
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-002: 导航与全局布局
**Description:** 作为访客，我希望通过清晰的导航快速切换各内容板块。

**Acceptance Criteria:**
- [ ] 顶部导航包含：Logo、电影、电视剧、动漫、影人、排行榜、搜索图标
- [ ] 当前页面对应导航项高亮显示
- [ ] 移动端（<768px）导航折叠为汉堡菜单，点击展开
- [ ] 页脚包含：关于我们、联系我们、版权声明、ICP 备案号占位符
- [ ] 全局布局在桌面端（>1024px）、平板（768–1024px）、手机（<768px）均无错位
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-003: 电影列表页
**Description:** 作为访客，我希望浏览完整的电影列表并进行多维度筛选，以找到符合我口味的电影。

**Acceptance Criteria:**
- [ ] 路由为 `/movies`，页面标题为「电影 - 全部」
- [ ] 以网格形式展示电影卡片，每页 24 条
- [ ] 提供筛选栏：
  - 类型（动作 / 喜剧 / 剧情 / 爱情 / 科幻 / 恐怖 / 纪录片 / 其他）
  - 地区（大陆 / 香港 / 台湾 / 欧美 / 日本 / 韩国 / 其他）
  - 年份（下拉，2000 年至今）
- [ ] 提供排序选项：综合评分、上映时间（最新/最早）
- [ ] 筛选与排序条件体现在 URL query params，支持链接分享
- [ ] 分页组件正常工作（上一页 / 下一页 / 页码跳转）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-004: 电视剧列表页
**Description:** 作为访客，我希望浏览电视剧列表并筛选，以找到想看的剧集。

**Acceptance Criteria:**
- [ ] 路由为 `/tv`，功能与电影列表页一致（参考 US-003）
- [ ] 筛选栏额外增加：完结状态（全部 / 连载中 / 已完结）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-005: 动漫列表页
**Description:** 作为访客，我希望浏览动漫列表并筛选。

**Acceptance Criteria:**
- [ ] 路由为 `/anime`，功能与电视剧列表页一致（参考 US-004）
- [ ] 筛选栏额外增加：来源地区细分（日本 / 中国 / 欧美）
- [ ] 提供「国漫」与「日漫」独立筛选入口（可通过来源地区快捷 Tab 实现）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-006: 电影详情页
**Description:** 作为访客，我希望查看一部电影的完整信息，以决定是否观看。

**Acceptance Criteria:**
- [ ] 路由为 `/movies/[id]`
- [ ] 页面顶部展示横幅背景图（模糊处理）+ 海报 + 基础信息区域
- [ ] 基础信息包含：
  - 中文标题、原文标题、别名（又名）
  - 豆瓣评分 + 评分人数 + 5星分布百分比
  - IMDB 评分 + IMDB 编号（可外链跳转）
  - 时光网分项评分（音乐 / 画面 / 导演 / 故事 / 表演，如有数据）
  - 上映日期（支持多地区多日期，含点映/公映标注）
  - 片长（支持多版本，如北美版/欧洲版）
  - 类型标签（多个）、制片国家/地区（多个）、语言
  - 导演、编剧、出品公司、发行公司
  - IMDb 编号（外链）
- [ ] 展示导演、主演（最多 10 人，含头像缩略图 + 姓名 + 角色名），「全部演职员」链接跳转 `/movies/[id]/credits`
- [ ] 剧情简介超过 150 字时折叠，点击「展开」显示全文
- [ ] 展示预告片（iframe 嵌入或外链跳转，无视频时隐藏该区域）
- [ ] 若该电影属于某系列，展示「所属系列」区块（系列名 + 系列内其他影片卡片列表，按上映时间排序）
- [ ] 展示「奖项」区块（奖项名称、届次、奖项类别、获奖人、获奖/提名状态；无奖项时隐藏）
- [ ] 展示「类型排名」标注（如「豆瓣恐怖片 No.4」，含跳转链接）
- [ ] 展示「相关推荐」（同类型影片，至少 6 部卡片）
- [ ] 演员头像点击跳转 `/people/[id]`
- [ ] 页面 `<title>` 格式为：`{影片名} ({年份}) - 影视网`
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-006d: 电影全部演职员页
**Description:** 作为访客，我希望查看一部电影的完整演职员列表。

**Acceptance Criteria:**
- [ ] 路由为 `/movies/[id]/credits`
- [ ] 按职务分组展示：导演 / 编剧 / 主演 / 配角 / 制片 / 摄影 / 音乐 / 其他
- [ ] 每人显示：头像缩略图、姓名、角色名或职务说明，点击跳转影人详情页
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-006b: 电影系列页
**Description:** 作为访客，我希望查看一个电影系列的全部作品，了解系列全貌。

**Acceptance Criteria:**
- [ ] 路由为 `/franchises/[id]`
- [ ] 展示系列名称、系列简介（如有）
- [ ] 以时间线或网格形式列出系列内所有电影，按上映时间排序
- [ ] 每部电影显示：海报、片名、上映年份、豆瓣评分、IMDB 评分
- [ ] 点击电影卡片跳转对应电影详情页
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-006c: 电影奖项页
**Description:** 作为访客，我希望查看某部电影的完整获奖与提名记录。

**Acceptance Criteria:**
- [ ] 路由为 `/movies/[id]/awards`（或在详情页内 Tab 展示）
- [ ] 按奖项分组展示：奖项名称、届次、具体奖项类别（最佳影片/最佳导演等）、获奖人、获奖/提名状态
- [ ] 支持按「获奖」/「提名」筛选
- [ ] 无奖项记录时显示「暂无奖项信息」
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-007: 电视剧详情页
**Description:** 作为访客，我希望查看一部电视剧的完整信息，包括分季与剧集详情。

**Acceptance Criteria:**
- [ ] 路由为 `/tv/[id]`，包含电影详情页所有基础模块（参考 US-006）
- [ ] 额外展示：首播日期、更新状态（连载中 / 已完结）、出品公司
- [ ] 展示分季列表（每季：季序号、季名、集数、首播年份、季海报）
- [ ] 点击某季跳转季详情页 `/tv/[id]/seasons/[season_number]`
- [ ] 季详情页展示该季所有集：集序号、集名、播出日期、集简介（可折叠）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-008: 动漫详情页
**Description:** 作为访客，我希望查看动漫的详细信息。

**Acceptance Criteria:**
- [ ] 路由为 `/anime/[id]`，包含电视剧详情页所有功能（参考 US-007）
- [ ] 季详情页路由为 `/anime/[id]/seasons/[season_number]`
- [ ] 额外展示：制作公司、原作来源（原创 / 漫画改编 / 小说改编，如有）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-009: 影人详情页
**Description:** 作为访客，我希望查看某位演员或导演的基本信息与作品列表。

**Acceptance Criteria:**
- [ ] 路由为 `/people/[id]`
- [ ] 展示基础信息：
  - 姓名（中文 + 英文 + 别名）
  - 头像、性别、出生日期、去世日期（如有）、出生地、国籍
  - 职业标签（演员 / 导演 / 编剧 / 制片人等，多职业）
  - IMDb 编号（外链）
  - 家庭成员（含关系标注，如有）
- [ ] 个人简介超过 150 字时折叠，点击展开
- [ ] 展示获奖记录列表（年份 + 奖项名称 + 奖项类别 + 关联影片 + 获奖/提名状态），超过 5 条显示「查看全部」
- [ ] 展示参演/执导作品列表，按时间倒序排列，分「全部 / 导演 / 编剧 / 演员」Tab 过滤
- [ ] 每条作品显示：海报缩略图、片名、年份、担任角色或职务
- [ ] 展示「合作过的影人」（合作次数最多的前 8 人，含头像 + 姓名 + 合作次数）
- [ ] 作品超过 20 条时显示「加载更多」按钮
- [ ] 作品卡片点击跳转对应影视详情页
- [ ] 页面 `<title>` 格式为：`{姓名} - 影人 - 影视网`
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-010: 全站搜索
**Description:** 作为访客，我希望通过关键词搜索影片或影人，快速定位目标内容。

**Acceptance Criteria:**
- [ ] 顶部导航搜索图标点击后在桌面端展开搜索框，移动端跳转搜索页
- [ ] 输入关键词后按回车或点击搜索按钮跳转至 `/search?q={keyword}`
- [ ] 搜索结果页分 Tab 显示：全部 / 电影 / 电视剧 / 动漫 / 影人
- [ ] 每条结果显示：封面/头像缩略图、标题、年份/出生年份、类型
- [ ] 无结果时显示「未找到与「{keyword}」相关的内容」提示
- [ ] URL query param `q` 值与搜索框内容保持同步
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-011: 排行榜页
**Description:** 作为访客，我希望查看当前热门与高分影视排行榜。

**Acceptance Criteria:**
- [ ] 路由为 `/rankings`
- [ ] 提供多个榜单 Tab：
  - 热门电影 / 热门电视剧 / 热门动漫（按近 7 日访问量）
  - 高分电影 / 高分电视剧 / 高分动漫（按豆瓣评分）
  - 分类型榜单（如「最高分恐怖片」「最高分喜剧」，参考豆瓣 typerank）
- [ ] 每个榜单展示前 50 条，带排名序号标识
- [ ] 每条显示：排名序号、海报缩略图、标题、年份、豆瓣评分、IMDB 评分、类型标签、简介前 60 字
- [ ] 分类型榜单支持按类型切换（动作 / 喜剧 / 恐怖 / 科幻 / 爱情 / 动画等）
- [ ] 榜单数据每日凌晨 2 点更新（定时任务）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-019: 奖项专题页
**Description:** 作为访客，我希望查看某个电影奖项的历届获奖记录，了解权威评选结果。

**Acceptance Criteria:**
- [ ] 路由为 `/awards/[slug]`（如 `/awards/oscar`），展示奖项简介、官网链接
- [ ] 路由为 `/awards/[slug]/[edition]`（如 `/awards/oscar/98`），展示某届完整提名与获奖名单
- [ ] 奖项类别分组展示（最佳影片 / 最佳导演 / 最佳男主角等）
- [ ] 每条记录显示：影片封面 + 片名（链接）+ 相关人员（链接）+ 获奖/提名标识
- [ ] 页面提供历届导航（上一届 / 下一届 / 届次下拉列表）
- [ ] 支持的奖项至少包含：奥斯卡、金球奖、戛纳电影节、威尼斯电影节、柏林电影节、金像奖、金马奖
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-012: 爬虫数据采集模块（后端）
**Description:** 作为开发者，我需要一个爬虫系统定期抓取影视数据并存入待审核区。

**Acceptance Criteria:**
- [ ] 实现针对至少 1 个目标数据源的爬虫脚本
- [ ] 爬取字段：标题（中/英）、封面图 URL、简介、类型、地区、年份、演职员列表
- [ ] 爬虫结果写入数据库 `pending_content` 暂存表（状态为 `pending`）
- [ ] 同时爬取豆瓣评分（`douban_score`）与 IMDB 评分（`imdb_score`），分别存储
- [ ] 海报、横幅、头像图片下载后上传至腾讯云 COS，数据库存储 COS 路径
- [ ] 请求频率控制：每次请求间隔 ≥ 3 秒，支持通过配置文件调整；每日单站点请求量可设上限
- [ ] 使用随机 User-Agent 池，支持配置 HTTP 代理列表
- [ ] 支持三个目标数据源：时光网、豆瓣电影、TMDB（通过 `--source` 参数指定）
- [ ] 支持命令行触发：`npm run crawl -- --type=movie|tv|anime`
- [ ] 每次爬取生成日志文件，记录：开始时间、抓取数量、成功数、失败数及原因
- [ ] 爬取失败的条目记录错误信息，不影响其他条目的处理
- [ ] Typecheck/lint passes

---

### US-013: 后台内容审核界面
**Description:** 作为内容管理员，我希望在后台审核爬虫抓取的内容，通过后才在前台展示。

**Acceptance Criteria:**
- [ ] 路由为 `/admin`，使用 HTTP Basic Auth 保护（用户名/密码通过环境变量配置）
- [ ] 列表页展示待审核内容：标题、类型、抓取时间、状态（待审核/已通过/已拒绝）
- [ ] 支持按类型（电影/电视剧/动漫）和状态筛选
- [ ] 点击某条目可查看详情（预览爬取到的全部字段）
- [ ] 支持单条「通过」/「拒绝」操作按钮
- [ ] 支持批量勾选后执行「批量通过」操作
- [ ] 通过后内容写入正式表并在前台可见；拒绝后状态更新为 `rejected` 且不展示
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-014: 后台手动新增影视内容
**Description:** 作为内容管理员，我希望在后台手动录入一部影视作品，无需经过爬虫流程直接发布。

**Acceptance Criteria:**
- [ ] 路由为 `/admin/content/new`，提供内容类型选择（电影 / 电视剧 / 动漫）
- [ ] 表单字段包含：中文标题、原文标题、简介、类型标签（多选）、地区、语言、上映/首播日期、评分（可选）、海报图上传或 URL 输入、预告片链接（可选）
- [ ] 电视剧/动漫额外字段：更新状态（连载中/已完结）、总集数
- [ ] 表单提交后内容直接写入正式表（状态为 `published`），无需审核流程
- [ ] 提交成功后跳转至该条目的编辑页，并显示「创建成功」提示
- [ ] 必填字段为空时提交按钮不可用，并显示行内错误提示
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-015: 后台编辑影视内容
**Description:** 作为内容管理员，我希望编辑已发布的影视条目，修正错误或补充信息。

**Acceptance Criteria:**
- [ ] 路由为 `/admin/content/[id]/edit`，表单预填当前数据
- [ ] 支持修改 US-014 中所有字段
- [ ] 支持管理演职员关联：搜索已有影人并添加为演员/导演，填写角色名或职务，可删除已有关联
- [ ] 保存后前台详情页数据实时更新（清除对应缓存）
- [ ] 保存成功显示「保存成功」提示；失败显示具体错误信息
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-016: 后台删除影视内容
**Description:** 作为内容管理员，我希望删除错误或下架的影视条目。

**Acceptance Criteria:**
- [ ] 在内容列表页和编辑页均提供「删除」按钮
- [ ] 点击删除弹出确认对话框，显示条目标题，需二次确认
- [ ] 确认后软删除（标记 `deleted_at`），前台不再展示，后台列表默认不显示已删除条目
- [ ] 后台提供「已删除」筛选项，可查看并恢复已删除条目
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-017: 后台手动新增与编辑影人
**Description:** 作为内容管理员，我希望手动维护影人档案，包括新增和编辑。

**Acceptance Criteria:**
- [ ] 新增路由 `/admin/people/new`，编辑路由 `/admin/people/[id]/edit`
- [ ] 表单字段：中文姓名、英文姓名、头像图上传或 URL 输入、性别、出生日期、出生地、国籍、职业标签（多选：演员/导演/编剧/配音/其他）、个人简介
- [ ] 编辑页预填当前数据，保存后前台影人详情页实时更新
- [ ] 支持删除影人（软删除，同时解除与影视作品的关联关系）
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

### US-018: 后台内容管理总览
**Description:** 作为内容管理员，我希望在后台有一个统一的内容管理入口，快速导航到各管理功能。

**Acceptance Criteria:**
- [ ] 路由 `/admin` 展示管理后台首页，包含数据统计卡片：电影总数、电视剧总数、动漫总数、影人总数、系列总数、待审核数量
- [ ] 提供快捷入口：新增电影、新增电视剧、新增动漫、新增影人、新增系列、待审核列表
- [ ] 左侧或顶部有后台导航菜单：概览 / 内容管理（电影/电视剧/动漫）/ 系列管理 / 影人管理 / 爬虫审核
- [ ] 内容管理各子页面提供列表视图：标题、类型、状态、创建时间、操作（编辑/删除）
- [ ] 列表支持按标题关键词搜索
- [ ] Typecheck/lint passes
- [ ] Verify in browser using dev-browser skill

---

## Functional Requirements

### 内容展示
- **FR-1:** 系统支持三种内容类型实体：Movie、TVSeries、Anime，各有独立路由前缀（`/movies`、`/tv`、`/anime`）
- **FR-2:** 每种内容类型有：列表页（含筛选/排序/分页）、详情页、全部演职员页
- **FR-3:** 影人（Person）有独立详情页 `/people/[id]`，展示基础档案、获奖记录、合作影人与关联作品列表
- **FR-4:** 详情页演职员头像/姓名可点击，跳转对应影人详情页
- **FR-5:** 所有列表页筛选与排序条件须体现在 URL query params 中，保证链接可分享
- **FR-6:** 列表页默认每页 24 条，支持分页导航
- **FR-7:** 所有图片须有语义化 `alt` 文本；图片加载失败显示灰色占位符
- **FR-24:** 电影支持归属系列（Franchise），系列有独立页面 `/franchises/[id]`，展示系列内所有电影
- **FR-25:** 电影支持奖项记录（Award），含奖项名称、届次、类别、获奖人、获奖/提名状态
- **FR-26:** 电视剧与动漫支持季（Season）与集（Episode）两级层级结构，季有独立详情页
- **FR-27:** 电影评分存储豆瓣评分（含5星分布）、IMDB 评分、时光网分项评分（音乐/画面/导演/故事/表演）
- **FR-28:** 电影上映日期支持多地区多日期记录（含点映/公映类型标注）
- **FR-29:** 电影片长支持多版本记录（如北美版/欧洲版）
- **FR-30:** 奖项有独立专题页（`/awards/[slug]/[edition]`），支持历届导航，至少覆盖奥斯卡、金球奖、戛纳、威尼斯、柏林、金像奖、金马奖
- **FR-31:** 排行榜支持分类型榜单（按类型 + 评分筛选，参考豆瓣 typerank 模式）
- **FR-32:** 影人页展示「合作过的影人」模块（合作次数最多的前 8 人）
- **FR-33:** 影人作品列表支持按职务 Tab 过滤（全部 / 导演 / 编剧 / 演员）

### 搜索
- **FR-8:** 全站搜索覆盖 Movie、TVSeries、Anime、Person 四类实体
- **FR-9:** 搜索结果按相关度排序，支持按类型 Tab 过滤

### 排行榜
- **FR-10:** 提供热门榜（按近 7 日访问量）与高分榜（按评分），每日定时更新

### 数据管理
- **FR-11:** 爬虫数据先存入 `pending_content` 暂存区，管理员审核通过后方可前台展示
- **FR-12:** 后台 `/admin` 路由须有 Basic Auth 身份验证保护
- **FR-13:** 数据库存储实体：影片基础信息、腾讯云 COS 图片路径、演职员关联关系、豆瓣评分（`douban_score`）与 IMDB 评分（`imdb_score`）
- **FR-22:** 动漫实体增加 `origin` 字段（`cn` 国漫 / `jp` 日漫 / `other`），列表页与首页均按此字段区分展示
- **FR-23:** 爬虫请求频率须可配置，默认间隔 ≥ 3 秒，支持随机 User-Agent 池与 HTTP 代理列表配置
- **FR-18:** 管理员可在后台直接新增 Movie / TVSeries / Anime / Person / Franchise，提交后状态为 `published`，无需审核
- **FR-19:** 管理员可编辑任意已发布条目的所有字段，包括演职员关联、奖项、季/集信息
- **FR-20:** 删除操作采用软删除（`deleted_at` 字段），前台不展示，后台可恢复
- **FR-21:** 后台内容列表支持按标题关键词搜索，并提供各内容类型的数量统计概览

### SEO 与性能
- **FR-14:** 每个详情页须有独立语义化的 `<title>` 与 `<meta name="description">`
- **FR-15:** 展示前端若 SEO 要求严格，建议采用 Nuxt 3（基于 Vue 3 的 SSR/SSG 框架）替代纯 Vite SPA
- **FR-16:** 图片列表中使用懒加载；首屏关键图片使用 `priority` 预加载
- **FR-17:** 核心页面在 Lighthouse 移动端 Performance 评分 ≥ 70，SEO 评分 ≥ 90

---

## Non-Goals（不在本期范围内）

- 用户注册、登录、个人中心功能
- 用户评分、短评、长评、点赞等社交互动功能
- 用户收藏夹、想看/在看/看过列表
- 在线视频播放、弹幕功能
- 付费会员或广告变现体系
- 移动端原生 App（iOS / Android）
- 多语言国际化（本期仅中文）
- 站内消息推送、邮件订阅
- 影人社交动态聚合（微博、微信等）
- 票房数据、上座率实时展示

---

## Design Considerations

- **整体风格：** 参考豆瓣电影（简洁、内容密度适中）与 TMDB（现代感、深色 Hero 背景）
- **配色：**
  - 导航栏：深色背景（`#1a1a2e` 或类似深蓝/黑）
  - 内容区：白色/浅灰背景
  - 强调色：橙黄色（评分星星、标签）
- **影视卡片：**
  - 海报：2:3 竖版比例（如 200×300px）
  - 横幅：16:9 比例（用于 Hero 与详情页背景）
- **排版：**
  - 中文字体栈：`PingFang SC, Microsoft YaHei, sans-serif`
  - 正文字号：16px，辅助信息：14px
- **响应式断点：**
  - 手机：< 768px（单列或双列卡片）
  - 平板：768px–1024px（三列卡片）
  - 桌面：> 1024px（四列及以上）

---

## Technical Considerations

| 层次 | 技术选型 |
|------|---------|
| 展示前端 | Vue 3 + TypeScript + Vite + Tailwind CSS |
| 管理前端 | Vue 3 + TypeScript + Vite + TDesign Starter + TDesign UI |
| 后端框架 | .NET Core 10 Web API |
| 架构模式 | DDD 多层架构（Domain / Application / Infrastructure / API） |
| ORM | SqlSugar |
| API 风格 | RESTful API |
| 数据库 | PostgreSQL |
| 缓存 | Redis（列表页、详情页缓存，TTL 1 小时） |
| 鉴权 | OAuth 2.0（管理后台） |
| 图片存储 | 腾讯云 COS（爬取图片下载后上传，前台通过 CDN 域名访问） |
| 爬虫 | Node.js + Cheerio 或 Python + Scrapy；目标站点：时光网、豆瓣电影、TMDB |

**分阶段实现计划：**

| 阶段 | 内容 | 包含 User Stories |
|------|------|-----------------|
| Phase 1 | 核心内容展示上线 | US-001, US-002, US-003, US-006, US-009 |
| Phase 2 | 完整品类 + 搜索 + 排行 | US-004, US-005, US-007, US-008, US-010, US-011 |
| Phase 3 | 数据采集与后台管理 | US-012, US-013, US-014, US-015, US-016, US-017, US-018 |

**数据库核心表（简要）：**
```
Movie / TVSeries / Anime（影视内容）
Franchise（电影系列）
AwardEvent / AwardCeremony / AwardNomination（奖项三级结构）
TVSeason / TVEpisode（电视剧季/集）
AnimeSeason / AnimeEpisode（动漫季/集）
Person（影人）
Credit（演职员关联）
PersonCollaboration（影人合作关系，预计算）
PendingContent（爬虫暂存区）
```

---

## Technical Architecture

> 以下采用问答形式说明各关键架构决策。

---

**Q: 整体架构是什么模式？**

前后端完全分离架构，分为三个独立项目：

- **展示前端**：Vue 3 + Vite，面向普通用户，纯静态部署（CDN/Nginx）
- **管理前端**：Vue 3 + Vite + TDesign Starter，面向内容管理员
- **后端 API**：.NET Core 10 Web API，采用 DDD 多层架构，提供统一 RESTful API

爬虫作为独立脚本，通过调用后端 API 或直连数据库写入 `pending_content` 暂存区。

```
┌──────────────────┐     ┌──────────────────┐
│   展示前端        │     │   管理前端        │
│ Vue3 + Vite      │     │ Vue3 + TDesign   │
│ Tailwind CSS     │     │ Starter          │
└────────┬─────────┘     └────────┬─────────┘
         │  RESTful API            │  RESTful API
         │  (公开接口)              │  (OAuth 2.0 保护)
         └──────────┬──────────────┘
                    │
┌───────────────────▼──────────────────────────┐
│         .NET Core 10 Web API                  │
│  ┌──────────────────────────────────────────┐ │
│  │  API Layer（Controllers / Middleware）    │ │
│  ├──────────────────────────────────────────┤ │
│  │  Application Layer（Services / DTOs）    │ │
│  ├──────────────────────────────────────────┤ │
│  │  Domain Layer（Entities / Aggregates）   │ │
│  ├──────────────────────────────────────────┤ │
│  │  Infrastructure Layer（SqlSugar / Redis）│ │
│  └──────────────────────────────────────────┘ │
└───────────────────┬──────────────────────────┘
                    │
         ┌──────────┴──────────┐
         │                     │
┌────────▼────────┐   ┌────────▼────────┐
│   PostgreSQL    │   │     Redis       │
│   （主数据库）   │   │   （缓存层）     │
└─────────────────┘   └─────────────────┘

┌─────────────────────────────────────────┐
│         爬虫服务（独立进程）               │
│  时光网 / 豆瓣 / TMDB → pending_content  │
│  图片 → 腾讯云 COS                       │
└─────────────────────────────────────────┘
```

---

**Q: 后端 DDD 分层如何划分？**

```
src/
├── AudioVideoFans.API/              # API 层
│   ├── Controllers/                 # RESTful 控制器
│   ├── Middleware/                  # 全局异常、日志、鉴权中间件
│   └── Program.cs
│
├── AudioVideoFans.Application/      # 应用层
│   ├── Services/                    # 业务用例（MovieService、PersonService 等）
│   ├── DTOs/                        # 请求/响应数据传输对象
│   └── Interfaces/                  # 应用服务接口
│
├── AudioVideoFans.Domain/           # 领域层
│   ├── Entities/                    # 领域实体（Movie、TVSeries、Anime、Person、Credit）
│   ├── Aggregates/                  # 聚合根
│   ├── ValueObjects/                # 值对象（Genre、Region 等）
│   └── Interfaces/                  # 仓储接口
│
└── AudioVideoFans.Infrastructure/   # 基础设施层
    ├── Repositories/                # SqlSugar 仓储实现
    ├── Cache/                       # Redis 缓存实现
    ├── Storage/                     # 腾讯云 COS 上传封装
    └── DbContext/                   # SqlSugar 数据库上下文
```

---

**Q: RESTful API 主要端点有哪些？**

```
# 公开接口（展示前端使用）
GET  /api/movies                              # 电影列表（筛选/排序/分页）
GET  /api/movies/{id}                         # 电影详情
GET  /api/movies/{id}/credits                 # 全部演职员
GET  /api/movies/{id}/awards                  # 电影奖项列表
GET  /api/franchises/{id}                     # 系列详情（含系列内所有电影）
GET  /api/tv                                  # 电视剧列表
GET  /api/tv/{id}                             # 电视剧详情
GET  /api/tv/{id}/seasons/{season_number}     # 电视剧季详情（含集列表）
GET  /api/anime                               # 动漫列表
GET  /api/anime/{id}                          # 动漫详情
GET  /api/anime/{id}/seasons/{season_number}  # 动漫季详情（含集列表）
GET  /api/people/{id}                         # 影人详情
GET  /api/people/{id}/collaborations          # 合作影人列表
GET  /api/awards                              # 奖项列表
GET  /api/awards/{slug}                       # 奖项简介 + 历届列表
GET  /api/awards/{slug}/{edition}             # 某届完整提名/获奖名单
GET  /api/search?q=&type=                     # 全站搜索
GET  /api/rankings?category=&type=            # 排行榜（含分类型榜单）

# 管理接口（OAuth 2.0 保护）
GET    /api/admin/stats                       # 数据统计概览
GET    /api/admin/content                     # 内容列表（含筛选/搜索）
POST   /api/admin/movies                      # 新增电影
PUT    /api/admin/movies/{id}                 # 编辑电影
DELETE /api/admin/movies/{id}                 # 软删除电影
POST   /api/admin/movies/{id}/awards          # 新增奖项提名
PUT    /api/admin/nominations/{id}            # 编辑提名记录
DELETE /api/admin/nominations/{id}            # 删除提名记录
POST   /api/admin/franchises                  # 新增系列
PUT    /api/admin/franchises/{id}             # 编辑系列
POST   /api/admin/award-events                # 新增奖项主表
POST   /api/admin/award-ceremonies            # 新增届次
PUT    /api/admin/award-ceremonies/{id}       # 编辑届次
POST   /api/admin/tv                          # 新增电视剧
PUT    /api/admin/tv/{id}
DELETE /api/admin/tv/{id}
POST   /api/admin/tv/{id}/seasons             # 新增季
PUT    /api/admin/seasons/{id}                # 编辑季
POST   /api/admin/seasons/{id}/episodes       # 新增集
PUT    /api/admin/episodes/{id}               # 编辑集
POST   /api/admin/anime                       # 新增动漫
PUT    /api/admin/anime/{id}
DELETE /api/admin/anime/{id}
POST   /api/admin/anime/{id}/seasons          # 新增动漫季
POST   /api/admin/people                      # 新增影人
PUT    /api/admin/people/{id}
DELETE /api/admin/people/{id}
GET    /api/admin/pending                     # 待审核列表
PUT    /api/admin/pending/{id}/approve        # 审核通过
PUT    /api/admin/pending/{id}/reject         # 审核拒绝
POST   /api/admin/pending/batch-approve       # 批量通过
```

---

**Q: 管理后台鉴权 OAuth 2.0 如何实现？**

采用 **OAuth 2.0 Resource Owner Password Credentials（密码模式）** 或 **Authorization Code + PKCE 模式**（推荐）。后端集成 `OpenIddict` 或 `IdentityServer` 作为授权服务器，颁发 JWT Access Token。

```
管理前端登录流程：
1. 用户输入账号密码 → 管理前端
2. 管理前端向 /connect/token 请求 Access Token
3. 后端验证凭据，返回 JWT（含 role: admin）
4. 管理前端将 Token 存入 localStorage / sessionStorage
5. 后续所有 /api/admin/* 请求携带 Authorization: Bearer {token}
6. API 层 Middleware 验证 Token 有效性与 admin 角色
```

Token 配置：
```
Access Token 有效期：2 小时
Refresh Token 有效期：7 天
算法：RS256
```

---

**Q: 展示前端与管理前端的技术差异是什么？**

| 维度 | 展示前端 | 管理前端 |
|------|---------|---------|
| 框架 | Vue 3 + Vite + Tailwind CSS | Vue 3 + Vite + TDesign Starter |
| UI 组件库 | 自定义组件为主 | TDesign Vue |
| 路由 | Vue Router（History 模式） | Vue Router（TDesign Starter 内置） |
| 状态管理 | Pinia | Pinia（TDesign Starter 内置） |
| 鉴权 | 无（公开访问） | OAuth 2.0 JWT，路由守卫拦截 |
| SEO | 需要（考虑 SSG 或 SSR，可用 Nuxt 3 替代） | 不需要 |
| 部署 | Nginx 静态托管 或 CDN | Nginx 静态托管（内网或受限访问） |

> **注意：** 展示前端若 SEO 要求严格（Lighthouse SEO ≥ 90），建议将 Vue 3 + Vite 替换为 **Nuxt 3**（基于 Vue 3 的 SSR/SSG 框架），其余技术栈不变。

---

**Q: 数据库表结构如何设计？**

核心表及关键字段如下：

```sql
-- 电影
Movie (id, title_cn, title_original, title_aliases[],
       synopsis, genres[], region[], language[],
       release_dates jsonb,           -- [{date, region, type: 点映|公映}]
       durations jsonb,               -- [{minutes, version: 北美版|欧洲版|...}]
       douban_score, douban_rating_count,
       douban_rating_dist jsonb,      -- {5star%, 4star%, 3star%, 2star%, 1star%}
       imdb_score, imdb_id,
       mtime_score_music, mtime_score_visual,
       mtime_score_director, mtime_score_story, mtime_score_performance,
       poster_cos_key, backdrop_cos_key, trailer_url,
       production_companies[], distributors[],
       franchise_id,
       status, deleted_at, created_at, updated_at)

-- 电影系列
Franchise (id, name, description, poster_cos_key,
           created_at, updated_at)

-- 奖项主表
AwardEvent (id, name_cn, name_en, slug,   -- slug: oscar / golden-globe / cannes
            official_url, created_at)

-- 奖项届次
AwardCeremony (id, award_event_id, edition, year, date,
               country, host_person_id, official_url)

-- 提名/获奖记录
AwardNomination (id, ceremony_id, category_name,
                 result,                  -- won | nominated
                 movie_id,               -- 可为空（影人奖）
                 person_ids[],           -- 获奖/提名人
                 created_at)

-- 电视剧
TVSeries (id, title_cn, title_original, title_aliases[],
          synopsis, genres[], region[], language[],
          air_status, first_air_date, production_companies[],
          douban_score, douban_rating_count,
          douban_rating_dist jsonb,
          imdb_score, imdb_id,
          poster_cos_key, backdrop_cos_key,
          status, deleted_at, created_at, updated_at)

-- 电视剧季
TVSeason (id, series_id, season_number, name,
          episode_count, first_air_date,
          poster_cos_key, overview)

-- 电视剧集
TVEpisode (id, season_id, episode_number, name,
           air_date, overview, duration_min)

-- 动漫
Anime (id, title_cn, title_original, title_aliases[],
       synopsis, genres[], origin,     -- cn | jp | other
       air_status, first_air_date, studio,
       source_material,               -- original | manga | novel | game | other
       douban_score, douban_rating_count,
       douban_rating_dist jsonb,
       imdb_score, imdb_id,
       poster_cos_key, backdrop_cos_key,
       status, deleted_at, created_at, updated_at)

-- 动漫季
AnimeSeason (id, anime_id, season_number, name,
             episode_count, first_air_date,
             poster_cos_key, overview)

-- 动漫集
AnimeEpisode (id, season_id, episode_number, name,
              air_date, overview, duration_min)

-- 影人
Person (id, name_cn, name_en, name_aliases[],
        gender, birth_date, death_date,
        birth_place, nationality,
        professions[],                -- director | writer | actor | producer | ...
        biography, imdb_id,
        family_members jsonb,         -- [{name, relation}]
        avatar_cos_key,
        deleted_at, created_at, updated_at)

-- 演职员关联
Credit (id, person_id, content_type, content_id,
        role,                         -- director | writer | actor | producer | ...
        character_name, display_order)

-- 影人合作关系（预计算，定期刷新）
PersonCollaboration (person_a_id, person_b_id, collaboration_count,
                     updated_at)

-- 爬虫暂存区
PendingContent (id, source, source_url, content_type,
                raw_data jsonb, review_status,
                reviewed_at, created_at)
```

`status` 枚举：`published` / `draft`；`deleted_at` 非空即为软删除。

---

**Q: 缓存策略是什么？**

| 场景 | 缓存方式 | TTL |
|------|---------|-----|
| 详情页数据 | Redis | 1 小时 |
| 列表页（含筛选参数） | Redis | 10 分钟 |
| 排行榜数据 | Redis | 24 小时（每日凌晨定时刷新） |
| 搜索结果 | 不缓存 | — |

管理员保存编辑后，Application Layer 主动删除对应 Redis key，保证前台数据实时更新。

---

**Q: 图片如何处理？**

爬虫下载原始图片 → 上传至腾讯云 COS → 数据库存储 COS object key。前台拼接 CDN 域名访问，便于后续更换存储桶而无需改数据库。

```
原始图片 URL
    ↓ 爬虫下载
腾讯云 COS（原图存储）
    ↓ CDN 加速
展示前端（<img> 标签直接引用 CDN URL）
```

---

**Q: 爬虫如何避免被封？**

```json
// crawl.config.json
{
  "requestInterval": 3000,
  "dailyLimitPerSite": 2000,
  "userAgents": ["..."],
  "proxies": ["..."],
  "retryTimes": 3,
  "retryDelay": 10000
}
```

TMDB 优先使用官方 API（需申请 API Key），豆瓣与时光网使用 HTML 解析。爬虫仅爬取公开页面，遵守各站 `robots.txt`。

---

**Q: 搜索如何实现？**

使用 PostgreSQL 全文搜索（`tsvector` + `tsquery`），对 `title_cn`、`title_original`、`synopsis` 建立 GIN 索引，支持中文分词（需安装 `zhparser` 扩展）。后端 Application Layer 封装搜索服务，无需引入 Elasticsearch，降低运维复杂度。

---

**Q: 部署架构是什么？**

| 服务 | 平台 | 说明 |
|------|------|------|
| 展示前端 | Nginx / CDN 静态托管 | 构建产物直接部署 |
| 管理前端 | Nginx 静态托管 | 建议限制访问 IP |
| .NET Core API | 腾讯云 CVM 或容器服务 | 反向代理到 Nginx |
| PostgreSQL | 腾讯云 CDB for PostgreSQL | 与 COS 同地域降低延迟 |
| Redis | 腾讯云 Redis | TTL 缓存 |
| 腾讯云 COS | 腾讯云 | 图片存储，绑定 CDN 域名 |
| 爬虫脚本 | 腾讯云 CVM 定时任务 | cron 触发，日志写本地文件 |

---

**Q: 环境变量有哪些？**

```bash
# 数据库
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...

# Redis
Redis__ConnectionString=...

# 腾讯云 COS
Tencent__Cos__SecretId=...
Tencent__Cos__SecretKey=...
Tencent__Cos__Bucket=...
Tencent__Cos__Region=ap-guangzhou
Tencent__Cos__CdnDomain=https://cdn.example.com

# OAuth 2.0
OAuth__Issuer=https://your-api-domain.com
OAuth__Audience=admin-client
OAuth__SigningKey=...          # RS256 私钥路径或内容

# TMDB API（爬虫用）
TMDB_API_KEY=...
```

---




- 所有详情页 Lighthouse SEO 评分 ≥ 90
- 首页 LCP（最大内容绘制）≤ 2.5s（移动端 4G 网络）
- 搜索结果页响应时间 ≤ 500ms（P95）
- 列表页筛选切换响应时间 ≤ 300ms
- 所有页面在 Chrome / Firefox / Safari 最新版本无布局错误
- Phase 1 上线时电影库存量 ≥ 500 部（含海报与简介）

---

## Open Questions ~~（已解答）~~

- **Q1 ✅:** 评分数据同时爬取豆瓣评分与 IMDB 评分，分别存储为独立字段（`douban_score`、`imdb_score`），前台双评分并列展示。
- **Q2 ✅:** 爬虫目标站点为**时光网、豆瓣电影、TMDB**。爬取时须控制请求频率（每次请求间隔 ≥ 3 秒，每日单站点请求量设上限），遵守各站 robots.txt，使用随机 User-Agent 与代理池降低封禁风险。
- **Q3 ✅:** 爬取的图片（海报、头像、横幅）下载后上传至**腾讯云 COS（对象存储）**，数据库只存储 COS 路径，前台通过 CDN 域名访问。
- **Q4 ✅:** 动漫分类区分**国漫**与**日漫**，在动漫列表页提供独立的来源地区筛选入口，首页「近期热门动漫」模块可按国漫/日漫分 Tab 展示。
- **Q5:** 暂不考虑种子数据预置。
- **Q6:** 暂不考虑域名与备案。
