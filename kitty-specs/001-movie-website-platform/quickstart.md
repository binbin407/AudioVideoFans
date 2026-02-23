# Quickstart: 影视资讯网站平台

**Feature**: 001-movie-website-platform
**Date**: 2026-02-21

---

## 前置条件

| 工具 | 版本 | 用途 |
|------|------|------|
| .NET SDK | 10.x | 后端 API |
| Node.js | 20 LTS | 前端构建 |
| Python | 3.11+ | 爬虫 |
| PostgreSQL | 15+ | 主数据库（需安装 zhparser） |
| Redis | 7.x | 缓存层 |
| Docker | 24+ | 本地开发容器（可选） |

---

## 仓库结构

```
/
├── frontend/    # Vue 3 + Vite + Tailwind CSS（展示前端）
├── admin/       # Vue 3 + Vite + TDesign Vue（管理后台）
├── api/         # .NET Core 10 Web API（DDD）
└── crawler/     # Python + Scrapy（爬虫）
```

---

## 本地开发启动

### 1. 数据库准备

```bash
# 使用 Docker 快速启动 PostgreSQL 15
docker run -d --name pg15 \
  -e POSTGRES_DB=moviesite \
  -e POSTGRES_USER=dev \
  -e POSTGRES_PASSWORD=dev123 \
  -p 5432:5432 \
  postgres:15

# 安装 zhparser（需要额外步骤，见 research.md 中的 zhparser 安装指南）
# 连接数据库后执行：
psql -U dev -d moviesite -c "CREATE EXTENSION IF NOT EXISTS zhparser;"

# 运行数据库迁移（在 api/ 目录）
cd api
dotnet ef database update
```

### 2. Redis 准备

```bash
docker run -d --name redis7 -p 6379:6379 redis:7-alpine
```

### 3. 后端 API 启动

```bash
cd api
cp appsettings.Development.json.example appsettings.Development.json
# 编辑 appsettings.Development.json，填入：
#   ConnectionStrings.Default (PostgreSQL)
#   Redis.Connection
#   COS.SecretId / COS.SecretKey / COS.Bucket / COS.Region
#   Auth.JwksUri (OAuth 2.0 JWKS 端点)

dotnet restore
dotnet run --project src/API
# API 默认运行在 https://localhost:5001
```

### 4. 展示前端启动

```bash
cd frontend
npm install
cp .env.example .env
# 编辑 .env：
#   VITE_API_BASE_URL=http://localhost:5001/api/v1
#   VITE_COS_CDN_BASE=https://your-cdn-domain.com

npm run dev
# 展示前端运行在 http://localhost:5173
```

### 5. 管理后台启动

```bash
cd admin
npm install
cp .env.example .env
# 编辑 .env：
#   VITE_API_BASE_URL=http://localhost:5001/api/v1
#   VITE_OAUTH_CLIENT_ID=...
#   VITE_OAUTH_REDIRECT_URI=http://localhost:5174/callback

npm run dev
# 管理后台运行在 http://localhost:5174
```

### 6. 爬虫运行

```bash
cd crawler
python -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\activate
pip install -r requirements.txt
cp settings.example.py settings_local.py
# 编辑 settings_local.py：
#   DATABASE_URL (PostgreSQL)
#   TMDB_API_KEY
#   PROXY_LIST (可选)

# 运行 TMDB 爬虫
scrapy crawl tmdb_spider -a content_type=movie -a ids=550,551,552
```

---

## 环境变量速查

### api/appsettings.Development.json 关键项

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=moviesite;Username=dev;Password=dev123"
  },
  "Redis": {
    "Connection": "localhost:6379"
  },
  "COS": {
    "SecretId": "",
    "SecretKey": "",
    "Bucket": "your-bucket-1234567890",
    "Region": "ap-guangzhou",
    "CdnBase": "https://your-cdn.com"
  },
  "Auth": {
    "JwksUri": "https://your-oauth-provider/.well-known/jwks.json",
    "Audience": "moviesite-api",
    "Issuer": "https://your-oauth-provider"
  },
  "Sentry": {
    "Dsn": ""
  }
}
```

### frontend/.env 关键项

```
VITE_API_BASE_URL=http://localhost:5001/api/v1
VITE_COS_CDN_BASE=https://your-cdn.com
```

---

## 运行测试

```bash
# 后端测试（xUnit）
cd api
dotnet test --collect:"XPlat Code Coverage"

# 前端测试（Vitest）
cd frontend
npm run test

# 爬虫测试（pytest）
cd crawler
pytest tests/
```

---

## 数据库迁移管理

```bash
# 新增迁移
cd api
dotnet ef migrations add AddMovieFranchise --project src/Infrastructure --startup-project src/API

# 应用迁移
dotnet ef database update --project src/Infrastructure --startup-project src/API

# 回滚到上一版本
dotnet ef database update PreviousMigrationName --project src/Infrastructure --startup-project src/API
```

---

## 腾讯云 COS 图片 URL 拼接规则

数据库中存储的是 COS object key（如 `posters/movie-123.jpg`），前端需拼接 CDN 域名：

```javascript
// frontend/src/utils/cos.ts
export const cosUrl = (key: string | null): string | null => {
  if (!key) return null
  return `${import.meta.env.VITE_COS_CDN_BASE}/${key}`
}
```

---

## 部署检查清单

- [ ] PostgreSQL 15 已安装 zhparser 扩展
- [ ] 所有数据库迁移已应用
- [ ] Redis 连接已配置
- [ ] 腾讯云 COS Bucket 与 CDN 域名已绑定
- [ ] OAuth 2.0 Provider 已配置，JWKS 端点可访问
- [ ] Sentry DSN 已配置（错误日志）
- [ ] Prometheus metrics 端点已暴露（`/metrics`）
- [ ] Nginx 反向代理配置完成（API + 前端静态文件）
- [ ] 定时任务已配置（排行榜更新：每日凌晨 2:00，popularity 更新：每日凌晨 2:30）
