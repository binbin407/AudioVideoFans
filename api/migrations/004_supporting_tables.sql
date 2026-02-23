CREATE TABLE IF NOT EXISTS media_videos (
  id BIGSERIAL PRIMARY KEY,
  content_type VARCHAR(20) NOT NULL,
  content_id BIGINT NOT NULL,
  title VARCHAR(300),
  url VARCHAR(1000) NOT NULL,
  type VARCHAR(30) NOT NULL,
  published_at DATE,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT media_videos_type_check CHECK (type IN ('trailer','teaser','clip','featurette','behind_the_scenes','bloopers')),
  CONSTRAINT media_videos_content_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX IF NOT EXISTS idx_media_videos_content ON media_videos(content_type, content_id);

CREATE TABLE IF NOT EXISTS award_events (
  id BIGSERIAL PRIMARY KEY,
  name_cn VARCHAR(200) NOT NULL,
  name_en VARCHAR(200),
  slug VARCHAR(100) NOT NULL UNIQUE,
  description TEXT,
  official_url VARCHAR(500),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS award_ceremonies (
  id BIGSERIAL PRIMARY KEY,
  event_id BIGINT NOT NULL REFERENCES award_events(id) ON DELETE CASCADE,
  edition_number INTEGER NOT NULL,
  year INTEGER NOT NULL,
  ceremony_date DATE,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT award_ceremonies_unique UNIQUE(event_id, edition_number)
);

CREATE INDEX IF NOT EXISTS idx_award_ceremonies_event ON award_ceremonies(event_id, edition_number);

CREATE TABLE IF NOT EXISTS award_nominations (
  id BIGSERIAL PRIMARY KEY,
  ceremony_id BIGINT NOT NULL REFERENCES award_ceremonies(id) ON DELETE CASCADE,
  category VARCHAR(200) NOT NULL,
  content_type VARCHAR(20),
  content_id BIGINT,
  person_id BIGINT REFERENCES people(id) ON DELETE SET NULL,
  is_winner BOOLEAN NOT NULL DEFAULT FALSE,
  note VARCHAR(500),
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT award_nominations_content_type_check CHECK (content_type IS NULL OR content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX IF NOT EXISTS idx_award_nominations_ceremony ON award_nominations(ceremony_id);
CREATE INDEX IF NOT EXISTS idx_award_nominations_content ON award_nominations(content_type, content_id) WHERE content_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_award_nominations_person ON award_nominations(person_id) WHERE person_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS featured_banners (
  id BIGSERIAL PRIMARY KEY,
  content_type VARCHAR(20) NOT NULL,
  content_id BIGINT NOT NULL,
  display_order INTEGER NOT NULL DEFAULT 0,
  start_at TIMESTAMPTZ,
  end_at TIMESTAMPTZ,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT featured_banners_type_check CHECK (content_type IN ('movie','tv_series','anime'))
);

CREATE INDEX IF NOT EXISTS idx_featured_banners_active ON featured_banners(display_order)
  WHERE (start_at IS NULL OR start_at <= NOW())
    AND (end_at IS NULL OR end_at > NOW())
    AND deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS pending_content (
  id BIGSERIAL PRIMARY KEY,
  source VARCHAR(20) NOT NULL,
  source_url VARCHAR(1000) NOT NULL UNIQUE,
  content_type VARCHAR(20) NOT NULL,
  raw_data JSONB NOT NULL,
  review_status VARCHAR(20) NOT NULL DEFAULT 'pending',
  reviewed_at TIMESTAMPTZ,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT pending_content_source_check CHECK (source IN ('douban','mtime','tmdb')),
  CONSTRAINT pending_content_status_check CHECK (review_status IN ('pending','approved','rejected')),
  CONSTRAINT pending_content_type_check CHECK (content_type IN ('movie','tv_series','anime','person'))
);

CREATE INDEX IF NOT EXISTS idx_pending_content_status ON pending_content(review_status, created_at DESC);

CREATE TABLE IF NOT EXISTS page_views (
  id BIGSERIAL PRIMARY KEY,
  content_type VARCHAR(20) NOT NULL,
  content_id BIGINT NOT NULL,
  viewed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_page_views_content_time ON page_views(content_type, content_id, viewed_at);
