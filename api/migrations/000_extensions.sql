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

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_ts_config_map m
    JOIN pg_ts_config c ON c.oid = m.mapcfg
    JOIN pg_ts_parser p ON p.oid = c.cfgparser
    JOIN pg_ts_token_type(p.prsname) t ON t.tokid = m.maptokentype
    WHERE c.cfgname = 'chinese_zh'
      AND t.alias IN ('n', 'v', 'a', 'i', 'e', 'l', 'j', 'h', 'k', 'x')
  ) THEN
    ALTER TEXT SEARCH CONFIGURATION chinese_zh
      ADD MAPPING FOR n, v, a, i, e, l, j, h, k, x WITH simple;
  END IF;
END
$$;
