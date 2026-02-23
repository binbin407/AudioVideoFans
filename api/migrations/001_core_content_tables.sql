CREATE TABLE IF NOT EXISTS franchises (
  id BIGSERIAL PRIMARY KEY,
  name_cn VARCHAR(200) NOT NULL,
  name_en VARCHAR(200),
  overview TEXT,
  poster_cos_key VARCHAR(500),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS movies (
  id BIGSERIAL PRIMARY KEY,
  title_cn VARCHAR(200) NOT NULL,
  title_original VARCHAR(200),
  title_aliases TEXT[] DEFAULT '{}',
  tagline TEXT,
  synopsis TEXT,
  genres TEXT[] DEFAULT '{}',
  region TEXT[] DEFAULT '{}',
  language TEXT[] DEFAULT '{}',
  release_dates JSONB DEFAULT '[]',
  durations JSONB DEFAULT '[]',
  douban_score DECIMAL(3,1),
  douban_rating_count INTEGER,
  douban_rating_dist JSONB,
  imdb_score DECIMAL(3,1),
  imdb_id VARCHAR(20),
  mtime_score_music DECIMAL(3,1),
  mtime_score_visual DECIMAL(3,1),
  mtime_score_director DECIMAL(3,1),
  mtime_score_story DECIMAL(3,1),
  mtime_score_performance DECIMAL(3,1),
  poster_cos_key VARCHAR(500),
  backdrop_cos_key VARCHAR(500),
  extra_backdrops TEXT[] DEFAULT '{}',
  extra_posters TEXT[] DEFAULT '{}',
  production_companies TEXT[] DEFAULT '{}',
  distributors TEXT[] DEFAULT '{}',
  franchise_id BIGINT REFERENCES franchises(id) ON DELETE SET NULL,
  franchise_order INTEGER,
  popularity INTEGER NOT NULL DEFAULT 0,
  status VARCHAR(20) NOT NULL DEFAULT 'published',
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  search_vector TSVECTOR
    GENERATED ALWAYS AS (
      setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')), 'A') ||
      setweight(to_tsvector('simple', coalesce(title_original, '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(array_to_string(title_aliases, ' '), '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')), 'C')
    ) STORED,
  CONSTRAINT movies_status_check CHECK (status IN ('published', 'draft'))
);

CREATE INDEX IF NOT EXISTS idx_movies_genres ON movies USING GIN(genres);
CREATE INDEX IF NOT EXISTS idx_movies_region ON movies USING GIN(region);
CREATE INDEX IF NOT EXISTS idx_movies_language ON movies USING GIN(language);
CREATE INDEX IF NOT EXISTS idx_movies_douban_score ON movies(douban_score DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_movies_popularity ON movies(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_movies_franchise ON movies(franchise_id) WHERE franchise_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_movies_status ON movies(status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_movies_fts ON movies USING GIN(search_vector) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS tv_series (
  id BIGSERIAL PRIMARY KEY,
  title_cn VARCHAR(200) NOT NULL,
  title_original VARCHAR(200),
  title_aliases TEXT[] DEFAULT '{}',
  synopsis TEXT,
  genres TEXT[] DEFAULT '{}',
  region TEXT[] DEFAULT '{}',
  language TEXT[] DEFAULT '{}',
  first_air_date DATE,
  last_air_date DATE,
  air_status VARCHAR(20),
  next_episode_info JSONB,
  number_of_seasons INTEGER DEFAULT 0,
  number_of_episodes INTEGER DEFAULT 0,
  douban_score DECIMAL(3,1),
  douban_rating_count INTEGER,
  douban_rating_dist JSONB,
  imdb_score DECIMAL(3,1),
  imdb_id VARCHAR(20),
  poster_cos_key VARCHAR(500),
  backdrop_cos_key VARCHAR(500),
  extra_backdrops TEXT[] DEFAULT '{}',
  extra_posters TEXT[] DEFAULT '{}',
  production_companies TEXT[] DEFAULT '{}',
  popularity INTEGER NOT NULL DEFAULT 0,
  status VARCHAR(20) NOT NULL DEFAULT 'published',
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  search_vector TSVECTOR
    GENERATED ALWAYS AS (
      setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')), 'A') ||
      setweight(to_tsvector('simple', coalesce(title_original, '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(array_to_string(title_aliases, ' '), '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')), 'C')
    ) STORED,
  CONSTRAINT tv_series_air_status_check CHECK (air_status IN ('airing','ended','production','cancelled')),
  CONSTRAINT tv_series_status_check CHECK (status IN ('published','draft'))
);

CREATE INDEX IF NOT EXISTS idx_tv_series_genres ON tv_series USING GIN(genres);
CREATE INDEX IF NOT EXISTS idx_tv_series_region ON tv_series USING GIN(region);
CREATE INDEX IF NOT EXISTS idx_tv_series_air_status ON tv_series(air_status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_tv_series_douban_score ON tv_series(douban_score DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_tv_series_popularity ON tv_series(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_tv_series_first_air ON tv_series(first_air_date DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_tv_series_fts ON tv_series USING GIN(search_vector) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS anime (
  id BIGSERIAL PRIMARY KEY,
  title_cn VARCHAR(200) NOT NULL,
  title_original VARCHAR(200),
  title_aliases TEXT[] DEFAULT '{}',
  synopsis TEXT,
  genres TEXT[] DEFAULT '{}',
  origin VARCHAR(10) NOT NULL DEFAULT 'other',
  source_material VARCHAR(30),
  studio VARCHAR(200),
  first_air_date DATE,
  last_air_date DATE,
  air_status VARCHAR(20),
  next_episode_info JSONB,
  number_of_seasons INTEGER DEFAULT 0,
  number_of_episodes INTEGER DEFAULT 0,
  douban_score DECIMAL(3,1),
  douban_rating_count INTEGER,
  douban_rating_dist JSONB,
  imdb_score DECIMAL(3,1),
  imdb_id VARCHAR(20),
  poster_cos_key VARCHAR(500),
  backdrop_cos_key VARCHAR(500),
  extra_backdrops TEXT[] DEFAULT '{}',
  extra_posters TEXT[] DEFAULT '{}',
  production_companies TEXT[] DEFAULT '{}',
  popularity INTEGER NOT NULL DEFAULT 0,
  status VARCHAR(20) NOT NULL DEFAULT 'published',
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  search_vector TSVECTOR
    GENERATED ALWAYS AS (
      setweight(to_tsvector('chinese_zh', coalesce(title_cn, '')), 'A') ||
      setweight(to_tsvector('simple', coalesce(title_original, '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(array_to_string(title_aliases, ' '), '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(synopsis, '')), 'C')
    ) STORED,
  CONSTRAINT anime_origin_check CHECK (origin IN ('cn','jp','other')),
  CONSTRAINT anime_source_material_check CHECK (source_material IN ('original','manga','novel','game')),
  CONSTRAINT anime_air_status_check CHECK (air_status IN ('airing','ended','production','cancelled')),
  CONSTRAINT anime_status_check CHECK (status IN ('published','draft'))
);

CREATE INDEX IF NOT EXISTS idx_anime_genres ON anime USING GIN(genres);
CREATE INDEX IF NOT EXISTS idx_anime_origin ON anime(origin) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_anime_source_material ON anime(source_material) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_anime_douban_score ON anime(douban_score DESC NULLS LAST) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_anime_popularity ON anime(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_anime_fts ON anime USING GIN(search_vector) WHERE deleted_at IS NULL;
