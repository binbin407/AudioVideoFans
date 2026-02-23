# Data Model: 影视资讯网站平台

**Feature**: 001-movie-website-platform
**Date**: 2026-02-21
**Database**: PostgreSQL 15+
**ORM**: SqlSugar (.NET Core 10)

---

## 概览

| 实体 | 表名 | 说明 |
|------|------|------|
| 电影 | `movies` | 独立实体 |
| 电视剧 | `tv_series` | 与 anime 完全独立表 |
| 动漫 | `anime` | 与 tv_series 完全独立表 |
| 电视剧季 | `tv_seasons` | 归属 tv_series |
| 电视剧集 | `tv_episodes` | 归属 tv_seasons |
| 动漫季 | `anime_seasons` | 归属 anime |
| 动漫集 | `anime_episodes` | 归属 anime_seasons |
| 影人 | `people` | 演员/导演/声优等 |
| 演职员关联 | `credits` | 多态：movie / tv_series / anime |
| 电影系列 | `franchises` | 系列主表 |
| 关键词 | `keywords` | 标签实体 |
| 内容-关键词 | `content_keywords` | 多态关联表 |
| 视频资源 | `media_videos` | 多态：预告片/花絮等 |
| 奖项 | `award_events` | 奖项主表（奥斯卡等） |
| 届次 | `award_ceremonies` | 归属 award_events |
| 提名/获奖记录 | `award_nominations` | 归属 award_ceremonies |
| 首页精选 | `featured_banners` | Hero Banner 配置 |
| 爬虫暂存 | `pending_content` | 审核队列 |

---

## 枚举值定义

```sql
-- 内容类型（多态字段复用）
-- content_type: 'movie' | 'tv_series' | 'anime'

-- 播出状态
-- air_status: 'airing' | 'ended' | 'production' | 'cancelled'

-- 内容状态
-- status: 'published' | 'draft'

-- 动漫来源
-- origin: 'cn' | 'jp' | 'other'

-- 原作类型
-- source_material: 'original' | 'manga' | 'novel' | 'game'

-- 职务角色
-- role: 'director' | 'writer' | 'actor' | 'producer' | 'cinematographer' |
--        'editor' | 'composer' | 'voice_actor' | ...

-- 爬虫审核状态
-- review_status: 'pending' | 'approved' | 'rejected'
```

---

## 表定义

### movies（电影）

```sql
CREATE TABLE movies (
    id                      BIGSERIAL PRIMARY KEY,
    title_cn                VARCHAR(200) NOT NULL,
    title_original          VARCHAR(200),
    title_aliases           TEXT[]          DEFAULT '{}',
    tagline                 TEXT,
    synopsis                TEXT,
    genres                  TEXT[]          DEFAULT '{}',
    region                  TEXT[]          DEFAULT '{}',
    language                TEXT[]          DEFAULT '{}',
    -- [{region: string, date: date, type: '首映'|'限定公映'|'正式公映'|'数字'|'实体'|'电视'}]
    release_dates           JSONB           DEFAULT '[]',
    -- [{version: string, minutes: int}]
    durations               JSONB           DEFAULT '[]',
    douban_score            DECIMAL(3,1),
    douban_rating_count     INTEGER,
    -- {five: float, four: float, three: float, two: float, one: float}  (百分比 0-100)
    douban_rating_dist      JSONB,
    imdb_score              DECIMAL(3,1),
    imdb_id                 VARCHAR(20),
    mtime_score_music       DECIMAL(3,1),
    mtime_score_visual      DECIMAL(3,1),
    mtime_score_director    DECIMAL(3,1),
    mtime_score_story       DECIMAL(3,1),
    mtime_score_performance DECIMAL(3,1),
    poster_cos_key          VARCHAR(500),
    backdrop_cos_key        VARCHAR(500),
    extra_backdrops         TEXT[]          DEFAULT '{}',
    extra_posters           TEXT[]          DEFAULT '{}',
    production_companies    TEXT[]          DEFAULT '{}',
    distributors            TEXT[]          DEFAULT '{}',
    franchise_id            BIGINT          REFERENCES franchises(id) ON DELETE SET NULL,
    franchise_order         INTEGER,        -- 在系列中的序号（第N部）
    popularity              INTEGER         NOT NULL DEFAULT 0,
    status                  VARCHAR(20)     NOT NULL DEFAULT 'published',
    deleted_at              TIMESTAMPTZ,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    -- 全文搜索向量（Generated Column，由数据库自动维护）
    search_vector           TSVECTOR
        GENERATED ALWAYS AS (
            setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')),       'A') ||
            setweight(to_tsvector('simple',     coalesce(title_original, '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(array_to_string(title_aliases, ' '), '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')),       'C')
        ) STORED,

    CONSTRAINT movies_status_check CHECK (status IN ('published', 'draft'))
);

-- 索引
CREATE INDEX idx_movies_genres         ON movies USING GIN(genres);
CREATE INDEX idx_movies_region         ON movies USING GIN(region);
CREATE INDEX idx_movies_language       ON movies USING GIN(language);
CREATE INDEX idx_movies_douban_score   ON movies(douban_score DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX idx_movies_popularity     ON movies(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX idx_movies_franchise      ON movies(franchise_id) WHERE franchise_id IS NOT NULL;
CREATE INDEX idx_movies_status         ON movies(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_movies_fts            ON movies USING GIN(search_vector) WHERE deleted_at IS NULL;
```

### tv_series（电视剧）

```sql
CREATE TABLE tv_series (
    id                      BIGSERIAL PRIMARY KEY,
    title_cn                VARCHAR(200) NOT NULL,
    title_original          VARCHAR(200),
    title_aliases           TEXT[]          DEFAULT '{}',
    synopsis                TEXT,
    genres                  TEXT[]          DEFAULT '{}',
    region                  TEXT[]          DEFAULT '{}',
    language                TEXT[]          DEFAULT '{}',
    first_air_date          DATE,
    last_air_date           DATE,
    air_status              VARCHAR(20),
    -- {air_date: date, title: string, season_number: int, episode_number: int}
    next_episode_info       JSONB,
    number_of_seasons       INTEGER         DEFAULT 0,
    number_of_episodes      INTEGER         DEFAULT 0,
    douban_score            DECIMAL(3,1),
    douban_rating_count     INTEGER,
    douban_rating_dist      JSONB,
    imdb_score              DECIMAL(3,1),
    imdb_id                 VARCHAR(20),
    poster_cos_key          VARCHAR(500),
    backdrop_cos_key        VARCHAR(500),
    extra_backdrops         TEXT[]          DEFAULT '{}',
    extra_posters           TEXT[]          DEFAULT '{}',
    production_companies    TEXT[]          DEFAULT '{}',
    popularity              INTEGER         NOT NULL DEFAULT 0,
    status                  VARCHAR(20)     NOT NULL DEFAULT 'published',
    deleted_at              TIMESTAMPTZ,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    search_vector           TSVECTOR
        GENERATED ALWAYS AS (
            setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')),       'A') ||
            setweight(to_tsvector('simple',     coalesce(title_original, '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(array_to_string(title_aliases, ' '), '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')),       'C')
        ) STORED,

    CONSTRAINT tv_series_air_status_check CHECK (air_status IN ('airing','ended','production','cancelled')),
    CONSTRAINT tv_series_status_check CHECK (status IN ('published','draft'))
);

CREATE INDEX idx_tv_series_genres       ON tv_series USING GIN(genres);
CREATE INDEX idx_tv_series_region       ON tv_series USING GIN(region);
CREATE INDEX idx_tv_series_air_status   ON tv_series(air_status) WHERE deleted_at IS NULL;
CREATE INDEX idx_tv_series_douban_score ON tv_series(douban_score DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX idx_tv_series_popularity   ON tv_series(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX idx_tv_series_first_air    ON tv_series(first_air_date DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX idx_tv_series_fts          ON tv_series USING GIN(search_vector) WHERE deleted_at IS NULL;
```

### anime（动漫）

```sql
CREATE TABLE anime (
    id                      BIGSERIAL PRIMARY KEY,
    title_cn                VARCHAR(200) NOT NULL,
    title_original          VARCHAR(200),
    title_aliases           TEXT[]          DEFAULT '{}',
    synopsis                TEXT,
    genres                  TEXT[]          DEFAULT '{}',
    origin                  VARCHAR(10)     NOT NULL DEFAULT 'other',
    source_material         VARCHAR(30),
    studio                  VARCHAR(200),
    first_air_date          DATE,
    last_air_date           DATE,
    air_status              VARCHAR(20),
    next_episode_info       JSONB,
    number_of_seasons       INTEGER         DEFAULT 0,
    number_of_episodes      INTEGER         DEFAULT 0,
    douban_score            DECIMAL(3,1),
    douban_rating_count     INTEGER,
    douban_rating_dist      JSONB,
    imdb_score              DECIMAL(3,1),
    imdb_id                 VARCHAR(20),
    poster_cos_key          VARCHAR(500),
    backdrop_cos_key        VARCHAR(500),
    extra_backdrops         TEXT[]          DEFAULT '{}',
    extra_posters           TEXT[]          DEFAULT '{}',
    production_companies    TEXT[]          DEFAULT '{}',
    popularity              INTEGER         NOT NULL DEFAULT 0,
    status                  VARCHAR(20)     NOT NULL DEFAULT 'published',
    deleted_at              TIMESTAMPTZ,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    search_vector           TSVECTOR
        GENERATED ALWAYS AS (
            setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')),       'A') ||
            setweight(to_tsvector('simple',     coalesce(title_original, '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(array_to_string(title_aliases, ' '), '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')),       'C')
        ) STORED,

    CONSTRAINT anime_origin_check CHECK (origin IN ('cn','jp','other')),
    CONSTRAINT anime_source_material_check CHECK (source_material IN ('original','manga','novel','game')),
    CONSTRAINT anime_air_status_check CHECK (air_status IN ('airing','ended','production','cancelled')),
    CONSTRAINT anime_status_check CHECK (status IN ('published','draft'))
);

CREATE INDEX idx_anime_genres         ON anime USING GIN(genres);
CREATE INDEX idx_anime_origin         ON anime(origin) WHERE deleted_at IS NULL;
CREATE INDEX idx_anime_source_material ON anime(source_material) WHERE deleted_at IS NULL;
CREATE INDEX idx_anime_douban_score   ON anime(douban_score DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX idx_anime_popularity     ON anime(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX idx_anime_fts            ON anime USING GIN(search_vector) WHERE deleted_at IS NULL;
```

### tv_seasons / anime_seasons（季）

```sql
CREATE TABLE tv_seasons (
    id              BIGSERIAL PRIMARY KEY,
    series_id       BIGINT NOT NULL REFERENCES tv_series(id) ON DELETE CASCADE,
    season_number   INTEGER NOT NULL,
    name            VARCHAR(200),
    episode_count   INTEGER DEFAULT 0,
    first_air_date  DATE,
    poster_cos_key  VARCHAR(500),
    overview        TEXT,
    vote_average    DECIMAL(3,1),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT tv_seasons_unique UNIQUE(series_id, season_number)
);

CREATE INDEX idx_tv_seasons_series ON tv_seasons(series_id, season_number);

-- 结构相同，仅外键指向 anime
CREATE TABLE anime_seasons (
    id              BIGSERIAL PRIMARY KEY,
    anime_id        BIGINT NOT NULL REFERENCES anime(id) ON DELETE CASCADE,
    season_number   INTEGER NOT NULL,
    name            VARCHAR(200),
    episode_count   INTEGER DEFAULT 0,
    first_air_date  DATE,
    poster_cos_key  VARCHAR(500),
    overview        TEXT,
    vote_average    DECIMAL(3,1),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT anime_seasons_unique UNIQUE(anime_id, season_number)
);

CREATE INDEX idx_anime_seasons_anime ON anime_seasons(anime_id, season_number);
```

### tv_episodes / anime_episodes（集）

```sql
CREATE TABLE tv_episodes (
    id              BIGSERIAL PRIMARY KEY,
    season_id       BIGINT NOT NULL REFERENCES tv_seasons(id) ON DELETE CASCADE,
    episode_number  INTEGER NOT NULL,
    name            VARCHAR(300),
    air_date        DATE,
    overview        TEXT,
    duration_min    INTEGER,
    still_cos_key   VARCHAR(500),
    vote_average    DECIMAL(3,1),

    CONSTRAINT tv_episodes_unique UNIQUE(season_id, episode_number)
);

CREATE INDEX idx_tv_episodes_season ON tv_episodes(season_id, episode_number);

CREATE TABLE anime_episodes (
    id              BIGSERIAL PRIMARY KEY,
    season_id       BIGINT NOT NULL REFERENCES anime_seasons(id) ON DELETE CASCADE,
    episode_number  INTEGER NOT NULL,
    name            VARCHAR(300),
    air_date        DATE,
    overview        TEXT,
    duration_min    INTEGER,
    still_cos_key   VARCHAR(500),
    vote_average    DECIMAL(3,1),

    CONSTRAINT anime_episodes_unique UNIQUE(season_id, episode_number)
);

CREATE INDEX idx_anime_episodes_season ON anime_episodes(season_id, episode_number);
```

### people（影人）

```sql
CREATE TABLE people (
    id              BIGSERIAL PRIMARY KEY,
    name_cn         VARCHAR(200) NOT NULL,
    name_en         VARCHAR(200),
    name_aliases    TEXT[]          DEFAULT '{}',
    gender          VARCHAR(10),
    birth_date      DATE,
    death_date      DATE,
    birth_place     VARCHAR(300),
    nationality     VARCHAR(100),
    height_cm       INTEGER,
    -- ['director','writer','actor','producer','voice_actor',...]
    professions     TEXT[]          DEFAULT '{}',
    biography       TEXT,
    imdb_id         VARCHAR(20),
    -- [{name: string, relation: string}]
    family_members  JSONB           DEFAULT '[]',
    avatar_cos_key  VARCHAR(500),
    photos_cos_keys TEXT[]          DEFAULT '{}',
    popularity      INTEGER         NOT NULL DEFAULT 0,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    search_vector   TSVECTOR
        GENERATED ALWAYS AS (
            setweight(to_tsvector('chinese_zh', coalesce(name_cn, '')),   'A') ||
            setweight(to_tsvector('simple',     coalesce(name_en, '')),   'A') ||
            setweight(to_tsvector('chinese_zh', coalesce(array_to_string(name_aliases, ' '), '')), 'B') ||
            setweight(to_tsvector('chinese_zh', coalesce(biography, '')), 'C')
        ) STORED
);

CREATE INDEX idx_people_popularity ON people(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX idx_people_professions ON people USING GIN(professions);
CREATE INDEX idx_people_fts ON people USING GIN(search_vector) WHERE deleted_at IS NULL;
```

### credits（演职员关联，多态）

```sql
CREATE TABLE credits (
    id              BIGSERIAL PRIMARY KEY,
    person_id       BIGINT NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    -- 多态：'movie' | 'tv_series' | 'anime'
    content_type    VARCHAR(20) NOT NULL,
    content_id      BIGINT NOT NULL,
    role            VARCHAR(50) NOT NULL,
    department      VARCHAR(50),
    character_name  VARCHAR(200),
    display_order   INTEGER DEFAULT 0,

    CONSTRAINT credits_content_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX idx_credits_person       ON credits(person_id);
CREATE INDEX idx_credits_content      ON credits(content_type, content_id);
CREATE INDEX idx_credits_person_content ON credits(person_id, content_type, content_id);
```

### franchises（电影系列）

```sql
CREATE TABLE franchises (
    id              BIGSERIAL PRIMARY KEY,
    name_cn         VARCHAR(200) NOT NULL,
    name_en         VARCHAR(200),
    overview        TEXT,
    poster_cos_key  VARCHAR(500),
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### keywords + content_keywords（关键词，多态）

```sql
CREATE TABLE keywords (
    id      BIGSERIAL PRIMARY KEY,
    name    VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE content_keywords (
    keyword_id   BIGINT NOT NULL REFERENCES keywords(id) ON DELETE CASCADE,
    content_type VARCHAR(20) NOT NULL,
    content_id   BIGINT NOT NULL,

    PRIMARY KEY (keyword_id, content_type, content_id),
    CONSTRAINT content_keywords_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX idx_content_keywords_content ON content_keywords(content_type, content_id);
```

### media_videos（视频资源，多态）

```sql
CREATE TABLE media_videos (
    id           BIGSERIAL PRIMARY KEY,
    content_type VARCHAR(20) NOT NULL,
    content_id   BIGINT NOT NULL,
    title        VARCHAR(300),
    url          VARCHAR(1000) NOT NULL,
    -- 'trailer'|'teaser'|'clip'|'featurette'|'behind_the_scenes'|'bloopers'
    type         VARCHAR(30) NOT NULL,
    published_at DATE,

    CONSTRAINT media_videos_type_check CHECK (type IN ('trailer','teaser','clip','featurette','behind_the_scenes','bloopers')),
    CONSTRAINT media_videos_content_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX idx_media_videos_content ON media_videos(content_type, content_id);
```

### award_events / award_ceremonies / award_nominations（奖项三级）

```sql
CREATE TABLE award_events (
    id          BIGSERIAL PRIMARY KEY,
    name_cn     VARCHAR(200) NOT NULL,
    name_en     VARCHAR(200),
    slug        VARCHAR(100) NOT NULL UNIQUE,   -- 'oscar','golden-globe','cannes'...
    description TEXT,
    official_url VARCHAR(500)
);

CREATE TABLE award_ceremonies (
    id              BIGSERIAL PRIMARY KEY,
    event_id        BIGINT NOT NULL REFERENCES award_events(id) ON DELETE CASCADE,
    edition_number  INTEGER NOT NULL,           -- 第N届
    year            INTEGER NOT NULL,
    ceremony_date   DATE,

    CONSTRAINT award_ceremonies_unique UNIQUE(event_id, edition_number)
);

CREATE INDEX idx_award_ceremonies_event ON award_ceremonies(event_id, edition_number);

CREATE TABLE award_nominations (
    id              BIGSERIAL PRIMARY KEY,
    ceremony_id     BIGINT NOT NULL REFERENCES award_ceremonies(id) ON DELETE CASCADE,
    category        VARCHAR(200) NOT NULL,      -- '最佳影片','最佳导演'...
    content_type    VARCHAR(20),                -- 关联内容（可空）
    content_id      BIGINT,
    person_id       BIGINT REFERENCES people(id) ON DELETE SET NULL,
    is_winner       BOOLEAN NOT NULL DEFAULT FALSE,
    note            VARCHAR(500)
);

CREATE INDEX idx_award_nominations_ceremony ON award_nominations(ceremony_id);
CREATE INDEX idx_award_nominations_content  ON award_nominations(content_type, content_id) WHERE content_id IS NOT NULL;
CREATE INDEX idx_award_nominations_person   ON award_nominations(person_id) WHERE person_id IS NOT NULL;
```

### featured_banners（首页精选）

```sql
CREATE TABLE featured_banners (
    id              BIGSERIAL PRIMARY KEY,
    content_type    VARCHAR(20) NOT NULL,
    content_id      BIGINT NOT NULL,
    display_order   INTEGER NOT NULL DEFAULT 0,
    start_at        TIMESTAMPTZ,    -- NULL 表示立即生效
    end_at          TIMESTAMPTZ,    -- NULL 表示永久有效
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT featured_banners_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX idx_featured_banners_active ON featured_banners(display_order)
    WHERE (start_at IS NULL OR start_at <= NOW()) AND (end_at IS NULL OR end_at > NOW());
```

### pending_content（爬虫暂存区）

```sql
CREATE TABLE pending_content (
    id              BIGSERIAL PRIMARY KEY,
    source          VARCHAR(20) NOT NULL,       -- 'douban'|'mtime'|'tmdb'
    source_url      VARCHAR(1000) NOT NULL UNIQUE,
    content_type    VARCHAR(20) NOT NULL,
    raw_data        JSONB NOT NULL,
    review_status   VARCHAR(20) NOT NULL DEFAULT 'pending',
    reviewed_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pending_content_source_check CHECK (source IN ('douban','mtime','tmdb')),
    CONSTRAINT pending_content_status_check CHECK (review_status IN ('pending','approved','rejected')),
    CONSTRAINT pending_content_type_check CHECK (content_type IN ('movie','tv_series','anime','person'))
);

CREATE INDEX idx_pending_content_status ON pending_content(review_status, created_at DESC);
```

---

## 全文搜索配置（zhparser）

```sql
-- 前提：已安装 zhparser 扩展
CREATE EXTENSION IF NOT EXISTS zhparser;
CREATE EXTENSION IF NOT EXISTS pg_trgm;  -- ILIKE 降级备用

-- 创建中文搜索配置（名称：chinese_zh）
CREATE TEXT SEARCH CONFIGURATION chinese_zh (PARSER = zhparser);
ALTER TEXT SEARCH CONFIGURATION chinese_zh
    ADD MAPPING FOR n, v, a, i, e, l, j, h, k, x WITH simple;

-- search_vector 为 Generated Column（STORED），数据库自动维护，无需触发器
-- 见各表定义中的 GENERATED ALWAYS AS ... STORED
```

---

## 相似内容查询（FR-41）

```sql
-- 给定 movie id，通过共有关键词找相似电影（优先），其次按 genre 重叠
WITH target_keywords AS (
    SELECT keyword_id FROM content_keywords
    WHERE content_type = 'movie' AND content_id = :movie_id
),
target_genres AS (
    SELECT genres FROM movies WHERE id = :movie_id
)
SELECT m.id, m.title_cn, m.poster_cos_key, m.douban_score,
       COUNT(ck.keyword_id) AS keyword_overlap
FROM movies m
LEFT JOIN content_keywords ck ON ck.content_type = 'movie' AND ck.content_id = m.id
    AND ck.keyword_id IN (SELECT keyword_id FROM target_keywords)
WHERE m.id <> :movie_id
  AND m.deleted_at IS NULL
  AND m.status = 'published'
GROUP BY m.id
ORDER BY keyword_overlap DESC, m.douban_score DESC NULLS LAST
LIMIT 6;
```

---

## 热度分更新（定时任务，每日凌晨 2 点）

```sql
-- 通过页面访问日志统计近 7 日 PV，更新各内容表 popularity 字段
-- page_views 表需单独记录（content_type, content_id, viewed_at）
UPDATE movies m
SET popularity = (
    SELECT COUNT(*) FROM page_views
    WHERE content_type = 'movie'
      AND content_id = m.id
      AND viewed_at >= NOW() - INTERVAL '7 days'
);
-- 同样适用于 tv_series、anime、people
```

---

## Redis 缓存键命名规范

| 缓存 Key 格式 | TTL | 说明 |
|-------------|-----|------|
| `movie:detail:{id}` | 1h | 电影详情 |
| `tv:detail:{id}` | 1h | 电视剧详情 |
| `anime:detail:{id}` | 1h | 动漫详情 |
| `person:detail:{id}` | 1h | 影人详情 |
| `movies:list:{hash}` | 10min | 电影列表（筛选条件 hash） |
| `tv:list:{hash}` | 10min | 电视剧列表 |
| `anime:list:{hash}` | 10min | 动漫列表 |
| `rankings:movie:score` | 24h | 电影高分榜 |
| `rankings:tv:score` | 24h | 电视剧高分榜 |
| `rankings:anime:score` | 24h | 动漫高分榜 |
| `rankings:movie:hot` | 24h | 电影热门榜 |
| `search:autocomplete:{q}` | 5min | 自动补全结果 |
| `home:banners` | 10min | 首页 Banner |

**清除规则**：管理员保存任意实体后，Application Layer 主动 `DEL` 对应 `detail` key 和相关 `list` keys。
