CREATE EXTENSION IF NOT EXISTS zhparser;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_ts_config
    WHERE cfgname = 'chinese_zh'
  ) THEN
    EXECUTE 'CREATE TEXT SEARCH CONFIGURATION chinese_zh (PARSER = zhparser)';
  END IF;
END
$$;

ALTER TEXT SEARCH CONFIGURATION chinese_zh
  ADD MAPPING FOR n, v, a, i, e, l, j, h, k, x WITH simple;
