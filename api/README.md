# API (后端服务)

技术栈：.NET 10 Web API + DDD + SqlSugar + PostgreSQL 15。

## 迁移文件

本目录 `migrations/` 在 WP01 提供数据库初始化 SQL：

- `000_extensions.sql`
- `001_core_content_tables.sql`
- `002_season_episode_tables.sql`
- `003_people_credits_keywords.sql`
- `004_supporting_tables.sql`

建议按文件名前缀顺序执行，确保扩展先于依赖它的表结构创建。

## zhparser 安装（PostgreSQL 15）

WP01 约定中文全文搜索配置名为 `chinese_zh`，由 `000_extensions.sql` 创建。

### 方案 A：使用已集成 zhparser 的 PostgreSQL 镜像

1. 拉起包含 `zhparser` 的 PostgreSQL 15 镜像。
2. 连接数据库并执行 `migrations/000_extensions.sql`。

### 方案 B：从源码编译 zhparser（Linux 容器）

参考项目：`https://github.com/amutu/zhparser`

典型步骤（示意）：

1. 安装 PostgreSQL 15 server dev headers 与构建工具。
2. 克隆 zhparser 源码并执行 `make && make install`。
3. 重启 PostgreSQL。
4. 执行 `migrations/000_extensions.sql`。

## 验证

```sql
SELECT cfgname FROM pg_ts_config WHERE cfgname = 'chinese_zh';
SELECT to_tsvector('chinese_zh', '星际穿越是一部科幻电影');
```

以上查询应返回配置项与分词结果。
