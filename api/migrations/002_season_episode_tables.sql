CREATE TABLE IF NOT EXISTS tv_seasons (
  id BIGSERIAL PRIMARY KEY,
  series_id BIGINT NOT NULL REFERENCES tv_series(id) ON DELETE CASCADE,
  season_number INTEGER NOT NULL,
  name VARCHAR(200),
  episode_count INTEGER DEFAULT 0,
  first_air_date DATE,
  poster_cos_key VARCHAR(500),
  overview TEXT,
  vote_average DECIMAL(3,1),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT tv_seasons_unique UNIQUE(series_id, season_number)
);

CREATE TABLE IF NOT EXISTS tv_episodes (
  id BIGSERIAL PRIMARY KEY,
  season_id BIGINT NOT NULL REFERENCES tv_seasons(id) ON DELETE CASCADE,
  episode_number INTEGER NOT NULL,
  name VARCHAR(300),
  air_date DATE,
  overview TEXT,
  duration_min INTEGER,
  still_cos_key VARCHAR(500),
  vote_average DECIMAL(3,1),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT tv_episodes_unique UNIQUE(season_id, episode_number)
);

CREATE TABLE IF NOT EXISTS anime_seasons (
  id BIGSERIAL PRIMARY KEY,
  anime_id BIGINT NOT NULL REFERENCES anime(id) ON DELETE CASCADE,
  season_number INTEGER NOT NULL,
  name VARCHAR(200),
  episode_count INTEGER DEFAULT 0,
  first_air_date DATE,
  poster_cos_key VARCHAR(500),
  overview TEXT,
  vote_average DECIMAL(3,1),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT anime_seasons_unique UNIQUE(anime_id, season_number)
);

CREATE TABLE IF NOT EXISTS anime_episodes (
  id BIGSERIAL PRIMARY KEY,
  season_id BIGINT NOT NULL REFERENCES anime_seasons(id) ON DELETE CASCADE,
  episode_number INTEGER NOT NULL,
  name VARCHAR(300),
  air_date DATE,
  overview TEXT,
  duration_min INTEGER,
  still_cos_key VARCHAR(500),
  vote_average DECIMAL(3,1),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT anime_episodes_unique UNIQUE(season_id, episode_number)
);

CREATE INDEX IF NOT EXISTS idx_tv_seasons_series ON tv_seasons(series_id, season_number);
CREATE INDEX IF NOT EXISTS idx_tv_episodes_season ON tv_episodes(season_id, episode_number);
CREATE INDEX IF NOT EXISTS idx_anime_seasons_anime ON anime_seasons(anime_id, season_number);
CREATE INDEX IF NOT EXISTS idx_anime_episodes_season ON anime_episodes(season_id, episode_number);
