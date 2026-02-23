CREATE TABLE IF NOT EXISTS people (
  id BIGSERIAL PRIMARY KEY,
  name_cn VARCHAR(200) NOT NULL,
  name_en VARCHAR(200),
  name_aliases TEXT[] DEFAULT '{}',
  gender VARCHAR(10),
  birth_date DATE,
  death_date DATE,
  birth_place VARCHAR(300),
  nationality VARCHAR(100),
  height_cm INTEGER,
  professions TEXT[] DEFAULT '{}',
  biography TEXT,
  imdb_id VARCHAR(20),
  family_members JSONB DEFAULT '[]',
  avatar_cos_key VARCHAR(500),
  photos_cos_keys TEXT[] DEFAULT '{}',
  popularity INTEGER NOT NULL DEFAULT 0,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  search_vector TSVECTOR
    GENERATED ALWAYS AS (
      setweight(to_tsvector('chinese_zh', coalesce(name_cn, '')), 'A') ||
      setweight(to_tsvector('simple', coalesce(name_en, '')), 'A') ||
      setweight(to_tsvector('chinese_zh', coalesce(array_to_string(name_aliases, ' '), '')), 'B') ||
      setweight(to_tsvector('chinese_zh', coalesce(biography, '')), 'C')
    ) STORED
);

CREATE INDEX IF NOT EXISTS idx_people_popularity ON people(popularity DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_people_professions ON people USING GIN(professions);
CREATE INDEX IF NOT EXISTS idx_people_fts ON people USING GIN(search_vector) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS credits (
  id BIGSERIAL PRIMARY KEY,
  person_id BIGINT NOT NULL REFERENCES people(id) ON DELETE CASCADE,
  content_type VARCHAR(20) NOT NULL,
  content_id BIGINT NOT NULL,
  role VARCHAR(50) NOT NULL,
  department VARCHAR(50),
  character_name VARCHAR(200),
  display_order INTEGER DEFAULT 0,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT credits_content_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX IF NOT EXISTS idx_credits_person ON credits(person_id);
CREATE INDEX IF NOT EXISTS idx_credits_content ON credits(content_type, content_id);
CREATE INDEX IF NOT EXISTS idx_credits_person_content ON credits(person_id, content_type, content_id);

CREATE TABLE IF NOT EXISTS keywords (
  id BIGSERIAL PRIMARY KEY,
  name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS content_keywords (
  keyword_id BIGINT NOT NULL REFERENCES keywords(id) ON DELETE CASCADE,
  content_type VARCHAR(20) NOT NULL,
  content_id BIGINT NOT NULL,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  PRIMARY KEY (keyword_id, content_type, content_id),
  CONSTRAINT content_keywords_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX IF NOT EXISTS idx_content_keywords_content ON content_keywords(content_type, content_id);
