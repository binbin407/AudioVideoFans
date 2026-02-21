# 影视资讯网站 项目宪法

> 由 spec-kitty constitution 命令自动生成
> 创建日期：2026-02-19
> 版本：1.0.0

## 目的

本宪法记录了「影视资讯网站」项目的技术标准、代码质量要求、团队约定与治理规范。所有功能开发与 Pull Request 均应遵循本宪法中的原则。

---

## 一、技术标准

### 语言与框架

| 层次 | 技术选型 |
|------|---------|
| 展示前端 | Vue 3 + TypeScript + Vite + Tailwind CSS |
| 管理前端 | Vue 3 + TypeScript + Vite + TDesign Vue |
| 后端框架 | .NET Core 10 Web API |
| 架构模式 | DDD 多层架构（Domain / Application / Infrastructure / API） |
| ORM | SqlSugar |
| API 风格 | RESTful API |
| 数据库 | PostgreSQL |
| 缓存 | Redis（TTL：详情页 1 小时，列表页 10 分钟，排行榜 24 小时） |
| 鉴权 | OAuth 2.0（管理后台，JWT RS256） |
| 图片存储 | 腾讯云 COS + CDN |
| 爬虫 | Node.js + Cheerio 或 Python + Scrapy |

**版本要求：**
- TypeScript：严格模式（`strict: true`）
- .NET Core：10.x
- Vue：3.x（Composition API 优先）
- PostgreSQL：15+

### 测试要求

- **后端**：使用 xUnit 进行单元测试与集成测试，代码覆盖率 ≥ 80%，核心业务路径（支付、鉴权、数据写入）覆盖率 100%
- **前端**：使用 Vitest 进行单元测试，关键组件（卡片、筛选器、分页）需有测试用例
- 所有测试须在 CI 流水线中自动运行，测试失败则阻断合并

### 性能目标

| 指标 | 目标值 |
|------|-------|
| Lighthouse 移动端 Performance | ≥ 70 |
| Lighthouse SEO 评分 | ≥ 75（SPA 限制：纯客户端渲染无法达到 SSR/SSG 级别的 ≥ 90，本项目采用 Vue 3 SPA，以 ≥ 75 为达标门槛） |
| 首页 LCP（移动端 4G） | ≤ 2.5s |
| 搜索结果响应时间（P95） | ≤ 500ms |
| 列表页筛选切换响应时间 | ≤ 300ms |
| Phase 1 上线电影库存量 | ≥ 500 部（含海报与简介） |

### 部署约束

| 服务 | 平台 |
|------|------|
| 展示前端 | Nginx / CDN 静态托管 |
| 管理前端 | Nginx 静态托管（建议限制访问 IP） |
| .NET Core API | 腾讯云 CVM 或容器服务，反向代理到 Nginx |
| PostgreSQL | 腾讯云 CDB for PostgreSQL |
| Redis | 腾讯云 Redis |
| 图片存储 | 腾讯云 COS，绑定 CDN 域名 |
| 爬虫脚本 | 腾讯云 CVM 定时任务（cron 触发） |

---

## 二、代码质量

### Pull Request 要求

- 至少 **1 人审批**，CI 检查全部通过后方可合并
- PR 描述须说明：变更目的、影响范围、测试方式
- 每个 PR 应聚焦单一功能或修复，避免大型混合 PR

### 代码审查清单

审查者须检查以下内容：

- [ ] 测试已添加或更新，覆盖新增逻辑
- [ ] TypeScript / C# 类型检查通过，无 `any` 滥用
- [ ] 无明显安全漏洞（SQL 注入、XSS、未授权访问等）
- [ ] 代码可读性良好，命名清晰，无魔法数字
- [ ] API 接口符合 RESTful 规范
- [ ] 缓存失效逻辑正确（编辑/删除后主动清除对应 Redis key）

### 质量门禁

合并前须满足：

- 所有测试通过
- 后端覆盖率 ≥ 80%
- TypeScript 类型检查通过（`tsc --noEmit`）
- C# 编译无警告
- ESLint / .NET Analyzer Lint 无报错

### 文档标准

- 后端所有公开 API 接口须有 XML 注释（用于 Swagger 自动生成文档）
- 新功能须更新对应 README 或接口文档
- 复杂业务逻辑须添加内联注释说明意图
- 架构级决策记录在 `docs/adr/` 目录下（ADR 格式）

---

## 三、团队约定

### 编码规范

**后端（.NET Core / DDD）：**
- 严格遵守 DDD 分层，禁止跨层直接调用（如 API 层不得直接访问 Repository）
- 领域实体不依赖基础设施层，保持纯净
- 所有外部输入（用户请求、API 参数）在 Application Layer 进行边界验证
- 缓存失效必须主动清除：管理员保存编辑后，Application Layer 删除对应 Redis key

**前端（Vue 3）：**
- 使用 Composition API（`<script setup>`），不使用 Options API
- 组件单一职责，单个组件不超过 300 行
- 所有列表页筛选与排序条件体现在 URL query params 中，保证链接可分享
- 图片必须有语义化 `alt` 文本；图片加载失败显示灰色占位符

**通用：**
- 数据库只存储腾讯云 COS object key，不存储完整 URL，前台拼接 CDN 域名访问
- 删除操作一律使用软删除（`deleted_at` 字段），禁止物理删除已发布内容
- 爬虫数据先进入 `pending_content` 暂存区，审核通过后方可前台展示

### 经验教训

以下架构决策已在 PRD 中确认，开发时须遵守：

1. **SEO 目标值已适配 SPA 架构**：本项目采用纯 Vite SPA（非 Nuxt 3 SSR/SSG），Lighthouse SEO 门槛定为 ≥ 75；如未来需提升至 ≥ 90 需迁移至 Nuxt 3
2. **边界验证在 API 层**：所有用户输入验证在 Application Layer 进行，Domain Layer 假设数据已合法
3. **缓存主动失效**：不依赖 TTL 自然过期，编辑操作后立即清除相关缓存 key
4. **图片存储解耦**：数据库存 COS key 而非完整 URL，便于后续更换存储桶或 CDN 域名
5. **搜索使用 PostgreSQL 全文搜索**：`tsvector` + `tsquery` + GIN 索引，无需引入 Elasticsearch，降低运维复杂度

---

## 四、治理规范

### 宪法修订流程

- 任何团队成员均可通过 Pull Request 提出宪法修订
- 修订 PR 须经至少 **1 人审批**后合并
- 重大变更（影响技术选型或架构模式）须在团队内讨论后决定
- 修订后更新文件头部的版本号与日期

### 合规验证

- **代码审查者**在 PR 审查时负责验证功能是否符合宪法要求
- 发现违规应在 PR 评论中指出，并在合并前修正
- 审查者有权拒绝不符合宪法要求的 PR

### 例外处理

- 例外情况须与团队讨论，提供充分技术理由
- 若例外情况频繁出现，应考虑更新宪法而非持续例外处理
- 例外决策记录在对应 PR 描述或 ADR 文档中
