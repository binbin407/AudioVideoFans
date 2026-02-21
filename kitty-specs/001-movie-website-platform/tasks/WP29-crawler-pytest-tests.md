---
work_package_id: "WP29"
subtasks:
  - "T127"
  - "T128"
  - "T129"
  - "T130"
title: "Crawler pytest Tests"
phase: "Phase 8 - Testing"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP13"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Created via /spec-kitty.analyze remediation (G1: crawler pytest gap)"
---

# Work Package Prompt: WP29 – Crawler pytest Tests

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP29 --base WP13
```

---

## Objectives & Success Criteria

- `pytest` test suite exists under `crawler/tests/`
- `pytest crawler/tests/` passes with zero failures
- Covers deduplication pipeline, normalization pipeline, PostgreSQL write pipeline, and spider item extraction
- CI job `crawler pytest` in `build-and-test.yml` (WP26 T115) runs cleanly
- No real HTTP requests in tests — all spider fetches mocked with static HTML fixtures

## Context & Constraints

- **Plan**: "pytest（爬虫管道）" listed as mandatory test tool (plan.md L19)
- **WP26 T115**: CI stub includes `crawler pytest` job — tests must exist for CI to pass
- Use `pytest`, `pytest-mock`, `responses` (HTTP mock library) or Scrapy's built-in `fake_response_from_file()` utility for spider tests
- Pipeline tests use an in-memory SQLite or mock psycopg2 connection — no real PostgreSQL required
- Fixture HTML files stored under `crawler/tests/fixtures/` (real Douban page snippets, anonymized)
- `pytest-cov` for coverage report: `pytest --cov=douban crawler/tests/`

---

## Subtasks & Detailed Guidance

### Subtask T127 – pytest Setup + Fixtures

**Purpose**: Initialize the pytest environment and shared test infrastructure.

**Steps**:
1. Add to `crawler/requirements.txt`:
   ```
   pytest>=8.0
   pytest-mock
   pytest-cov
   responses
   ```
2. Create `crawler/tests/` directory with:
   - `__init__.py`
   - `conftest.py` — shared fixtures:
     ```python
     import pytest
     from scrapy.http import HtmlResponse, Request

     def fake_response(url: str, fixture_file: str) -> HtmlResponse:
         """Load a static HTML fixture and wrap it as a Scrapy HtmlResponse."""
         fixture_path = Path(__file__).parent / "fixtures" / fixture_file
         body = fixture_path.read_bytes()
         request = Request(url=url)
         return HtmlResponse(url=url, request=request, body=body, encoding="utf-8")

     @pytest.fixture
     def movie_response():
         return fake_response(
             "https://movie.douban.com/subject/1292052/",
             "douban_movie_1292052.html"
         )

     @pytest.fixture
     def mock_db(mocker):
         """Mock psycopg2 connection to avoid real DB writes in pipeline tests."""
         return mocker.patch("douban.pipelines.psycopg2.connect")
     ```
3. `crawler/tests/fixtures/`: add 3 minimal HTML fixture files:
   - `douban_movie_1292052.html` — trimmed Shawshank Redemption page (keep only the CSS selectors used by the spider)
   - `douban_tv_sample.html` — trimmed TV series page with `air_status` and season info
   - `douban_anime_sample.html` — trimmed anime page with production country field
4. `crawler/pytest.ini` (or `pyproject.toml` `[tool.pytest.ini_options]`):
   ```ini
   [pytest]
   testpaths = tests
   python_files = test_*.py
   ```

**Files**:
- `crawler/tests/__init__.py`
- `crawler/tests/conftest.py`
- `crawler/tests/fixtures/douban_movie_1292052.html`
- `crawler/tests/fixtures/douban_tv_sample.html`
- `crawler/tests/fixtures/douban_anime_sample.html`
- `crawler/pytest.ini`

**Validation**:
- [ ] `cd crawler && pytest --collect-only` shows test items (no import errors)
- [ ] `fake_response()` helper returns a valid `HtmlResponse` object
- [ ] `mock_db` fixture patches psycopg2 without touching real DB

---

### Subtask T128 – Spider Extraction Tests

**Purpose**: Unit-test CSS/XPath selectors for each spider using static HTML fixtures.

**Steps**:
1. `crawler/tests/test_spider_movie.py`:
   ```python
   from douban.spiders.douban_movie import DoubanMovieSpider

   class TestDoubanMovieSpider:
       def setup_method(self):
           self.spider = DoubanMovieSpider()

       def test_extracts_title_cn(self, movie_response):
           items = list(self.spider.parse(movie_response))
           assert items[0]["raw_data"]["title_cn"] == "肖申克的救赎"

       def test_extracts_douban_score(self, movie_response):
           items = list(self.spider.parse(movie_response))
           score = float(items[0]["raw_data"]["douban_score"])
           assert 9.0 <= score <= 10.0

       def test_extracts_genres_as_list(self, movie_response):
           items = list(self.spider.parse(movie_response))
           genres = items[0]["raw_data"]["genres"]
           assert isinstance(genres, list)
           assert len(genres) >= 1

       def test_sets_content_type_movie(self, movie_response):
           items = list(self.spider.parse(movie_response))
           assert items[0]["content_type"] == "movie"

       def test_sets_source_url(self, movie_response):
           items = list(self.spider.parse(movie_response))
           assert "douban.com/subject/1292052" in items[0]["source_url"]
   ```
2. `crawler/tests/test_spider_tv.py`:
   - `test_extracts_air_status`: verify `air_status` in `{connecting, ended, production}`
   - `test_extracts_first_air_date`: verify date string format `YYYY-MM-DD`
3. `crawler/tests/test_spider_anime.py`:
   - `test_sets_origin_cn_for_chinese_anime`: fixture has 中国 in production → `origin == "cn"`
   - `test_sets_origin_jp_for_japanese_anime`: fixture has 日本 → `origin == "jp"`

**Files**:
- `crawler/tests/test_spider_movie.py`
- `crawler/tests/test_spider_tv.py`
- `crawler/tests/test_spider_anime.py`

**Validation**:
- [ ] All 5 movie spider assertions pass against fixture HTML
- [ ] `origin` field set correctly for both CN and JP anime fixtures
- [ ] No real HTTP requests made during test run (verified by `responses` library if needed)

---

### Subtask T129 – Pipeline Unit Tests

**Purpose**: Test the deduplication and PostgreSQL write pipelines with mocked DB connections.

**Steps**:
1. `crawler/tests/test_pipeline_dedup.py`:
   ```python
   from douban.pipelines import DeduplicationPipeline

   class TestDeduplicationPipeline:
       def test_passes_new_item(self, mock_db, mocker):
           """Item with unseen source_url is not dropped."""
           pipeline = DeduplicationPipeline()
           # Mock DB query returns 0 existing rows
           mock_db.return_value.cursor.return_value.fetchone.return_value = (0,)
           item = {"source_url": "https://movie.douban.com/subject/999/", "content_type": "movie"}
           result = pipeline.process_item(item, spider=None)
           assert result == item  # item passed through

       def test_drops_duplicate_item(self, mock_db, mocker):
           """Item with existing source_url raises DropItem."""
           from scrapy.exceptions import DropItem
           pipeline = DeduplicationPipeline()
           # Mock DB query returns 1 existing row
           mock_db.return_value.cursor.return_value.fetchone.return_value = (1,)
           item = {"source_url": "https://movie.douban.com/subject/1292052/", "content_type": "movie"}
           with pytest.raises(DropItem):
               pipeline.process_item(item, spider=None)

       def test_allows_rejected_item_resubmission(self, mock_db):
           """Rejected items (review_status=rejected) can be re-submitted."""
           pipeline = DeduplicationPipeline()
           # Mock: existing row has review_status='rejected'
           mock_db.return_value.cursor.return_value.fetchone.return_value = (0,)  # dedup query excludes rejected
           item = {"source_url": "https://movie.douban.com/subject/1292052/", "content_type": "movie"}
           result = pipeline.process_item(item, spider=None)
           assert result == item
   ```
2. `crawler/tests/test_pipeline_postgres.py`:
   - `test_inserts_pending_content_row`: verify `cursor.execute` called with INSERT INTO pending_content
   - `test_commit_called_after_insert`: verify `connection.commit()` called
   - `test_closes_connection_on_spider_closed`: verify `spider_closed()` calls `connection.close()`
   - `test_raw_data_stored_as_json`: verify `raw_data` param is a JSON string (not a dict)

**Files**:
- `crawler/tests/test_pipeline_dedup.py`
- `crawler/tests/test_pipeline_postgres.py`

**Validation**:
- [ ] `DropItem` raised for duplicate `source_url` (non-rejected)
- [ ] Rejected items not treated as duplicates
- [ ] INSERT statement verified without touching real PostgreSQL
- [ ] Connection lifecycle (`open`/`commit`/`close`) fully tested

---

### Subtask T130 – Settings + Middleware Tests

**Purpose**: Verify critical settings values and the User-Agent rotation middleware.

**Steps**:
1. `crawler/tests/test_settings.py`:
   ```python
   import importlib
   from douban import settings

   def test_download_delay_at_least_3():
       """FR-28: default crawler interval must be ≥ 3 seconds."""
       assert settings.DOWNLOAD_DELAY >= 3

   def test_robotstxt_obeyed():
       assert settings.ROBOTSTXT_OBEY is True

   def test_pipelines_ordered_correctly():
       pipelines = settings.ITEM_PIPELINES
       dedup_order = pipelines.get("douban.pipelines.DeduplicationPipeline")
       postgres_order = pipelines.get("douban.pipelines.PostgresPipeline")
       assert dedup_order < postgres_order  # dedup runs before write
   ```
2. `crawler/tests/test_middleware_useragent.py`:
   - `test_ua_middleware_rotates_user_agent`: call `process_request()` 10 times, verify at least 2 distinct User-Agent strings used
   - `test_ua_middleware_never_empty`: verify no request has empty User-Agent header

**Files**:
- `crawler/tests/test_settings.py`
- `crawler/tests/test_middleware_useragent.py`

**Validation**:
- [ ] `DOWNLOAD_DELAY >= 3` test enforces FR-28 at code level (regression guard)
- [ ] `ROBOTSTXT_OBEY` confirmed True in settings
- [ ] Pipeline ordering assertion catches accidental reordering
- [ ] UA rotation test verifies middleware is active

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Douban HTML structure changes break fixture-based tests | Keep fixtures minimal (only the CSS selectors used); add comment in fixture files noting which selectors are relied upon |
| psycopg2 mock call depth is complex | Use `mocker.MagicMock()` with chained return values; document mock chain in conftest |
| Spider `parse()` returns a generator with Request objects mixed in | Filter items with `isinstance(item, DoubanContentItem)` in test assertions |

## Review Guidance

- `test_download_delay_at_least_3` is a **regression guard** — if someone accidentally sets `DOWNLOAD_DELAY = 1`, CI will catch it immediately
- Fixture HTML files should be as minimal as possible: only the HTML tags that the spiders' CSS selectors target, not full page dumps
- Do NOT mock `DOWNLOAD_DELAY` enforcement in the spider itself — test the settings module directly (T130)
- CI integration: `crawler pytest` job in `build-and-test.yml` should run `cd crawler && pytest --cov=douban tests/`

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Created via analyze remediation (G1).
