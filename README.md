# 影视资讯网站平台 Monorepo

本仓库采用 Monorepo 组织，包含 4 个独立子系统：

- `frontend/`：展示前端（Vue 3 + TypeScript + Vite + Tailwind CSS）
- `admin/`：管理后台（Vue 3 + TypeScript + Vite + TDesign Vue）
- `api/`：后端 API（.NET 10 Web API + DDD + SqlSugar）
- `crawler/`：数据爬虫（Python 3.11 + Scrapy）

## 当前阶段

本工作区为 `WP01`，目标是完成：

- Monorepo 初始目录骨架
- PostgreSQL 基础 Schema 迁移脚本
- zhparser 全文检索扩展与配置

## 快速导航

- 展示前端说明：`frontend/README.md`
- 管理后台说明：`admin/README.md`
- 后端说明与迁移：`api/README.md`
- 爬虫说明：`crawler/README.md`
