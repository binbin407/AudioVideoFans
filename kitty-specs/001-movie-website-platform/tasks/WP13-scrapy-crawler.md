---
work_package_id: WP13
title: Scrapy Crawler
lane: planned
dependencies:
- WP01
subtasks:
- T053
- T054
- T055
- T056
- T057
- T058
- T059
phase: Phase 3 - Crawler
assignee: ''
agent: ''
shell_pid: ''
review_status: ''
reviewed_by: ''
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP13 – Scrapy Crawler

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP13 --base WP03
```

---

## Objectives & Success Criteria

- Scrapy project under `/crawler` scrapes Douban movie/TV/anime/person pages
- Scraped data stored in `pending_content` table via direct PostgreSQL insert (bypasses API)
- Pipelines handle deduplication, field normalization, and image URL extraction
- Spider can be run manually: `scrapy crawl douban_movie -a ids=123,456`
- Duplicate detection: skip if `pending_content` already has same `source_url` with `review_status != 'rejected'`

## Context & Constraints

- **Spec**: FR-38 (crawler submits to pending queue), FR-39 (deduplication)
- Crawler writes directly to PostgreSQL `pending_content` table (not via HTTP API) for performance
- `raw_data` JSONB must match the shape expected by admin approve flow (WP11 T046)
- Use `scrapy-playwright` for JS-rendered pages if needed; default to `scrapy` HTTP for static pages
- Respect `robots.txt`; add 2-second delay between requests (`DOWNLOAD_DELAY = 2`)
- Store COS image keys as URLs for now (admin manually uploads images; crawler stores original URLs in `raw_data`)

## Subtasks & Detailed Guidance

### Subtask T053 – Scrapy Project Scaffold

**Purpose**: Initialize the crawler project structure with settings and shared utilities.

**Steps**:
1. Create `/crawler` directory with standard Scrapy layout:
   ```
   crawler/
     scrapy.cfg
     douban/
       __init__.py
       settings.py
       items.py
       pipelines.py
       middlewares.py
       spiders/
         __init__.py
         douban_movie.py
         douban_tv.py
         douban_anime.py
         douban_person.py
   ```
2. `settings.py` key settings:
   ```python
   DOWNLOAD_DELAY = 2
   ROBOTSTXT_OBEY = True
   ITEM_PIPELINES = {
       'douban.pipelines.DeduplicationPipeline': 100,
       'douban.pipelines.NormalizationPipeline': 200,
       'douban.pipelines.PostgresPipeline': 300,
   }
   # DB connection from env
   DATABASE_URL = os.environ.get('DATABASE_URL')
   ```
3. `items.py`: define `DoubanContentItem` with fields matching `pending_content.raw_data` shape.
4. `requirements.txt`: `scrapy>=2.11`, `psycopg2-binary`, `python-dotenv`.

**Files**:
- `crawler/scrapy.cfg`
- `crawler/douban/settings.py`
- `crawler/douban/items.py`
- `crawler/requirements.txt`

**Validation**:
- [ ] `cd crawler && scrapy list` shows all 4 spiders
- [ ] Settings load `DATABASE_URL` from environment

---

### Subtask T054 – Douban Movie Spider

**Purpose**: Scrape Douban movie detail pages and extract structured data.

**Steps**:
1. `douban_movie.py` spider:
   - Start URLs: `https://movie.douban.com/subject/{id}/`
   - Accept `-a ids=123,456` argument to scrape specific IDs.
   - Extract fields:
     - `title_cn`: `h1 span[property="v:itemreviewed"]`
     - `douban_score`: `strong.rating_num`
     - `douban_rating_count`: `span[property="v:votes"]`
     - `genres`: `span[property="v:genre"]` (list)
     - `release_dates`: parse from info section (date + region)
     - `duration_min`: `span[property="v:runtime"]`
     - `synopsis`: `span[property="v:summary"]`
     - `poster_url`: `img[rel="v:image"]` src attribute
     - `directors`: `a[rel="v:directedBy"]` (list of names)
     - `cast`: `a[rel="v:starring"]` (list of names, max 20)
2. Yield `DoubanContentItem` with `content_type='movie'`, `source_url`, `raw_data` dict.

**Files**:
- `crawler/douban/spiders/douban_movie.py`

**Validation**:
- [ ] `scrapy crawl douban_movie -a ids=1292052` scrapes The Shawshank Redemption
- [ ] Item contains `title_cn`, `douban_score`, `genres` list

---

### Subtask T055 – Douban TV + Anime Spiders

**Purpose**: Scrape TV series and anime pages (similar structure to movie spider).

**Steps**:
1. `douban_tv.py`: same structure as movie spider; additional fields:
   - `air_status`: parse from info section (连载中/已完结)
   - `number_of_seasons`: from episode guide section
   - `first_air_date`, `last_air_date`
2. `douban_anime.py`: extends TV spider logic; additional fields:
   - `origin`: detect from production country (中国→cn, 日本→jp, else→other)
   - `source_material`: parse from tags if available
3. Both spiders accept `-a ids=` argument.

**Files**:
- `crawler/douban/spiders/douban_tv.py`
- `crawler/douban/spiders/douban_anime.py`

**Validation**:
- [ ] TV spider extracts `air_status` field
- [ ] Anime spider sets `origin` based on production country

---

### Subtask T056 – Douban Person Spider

**Purpose**: Scrape person (actor/director) profile pages.

**Steps**:
1. `douban_person.py`:
   - Start URL: `https://movie.douban.com/celebrity/{id}/`
   - Extract: `name_cn`, `name_en`, `gender`, `birth_date`, `birth_place`, `professions` (list), `biography`, `avatar_url`
   - `professions`: parse from info section (导演/演员/编剧 etc.)
2. Yield `DoubanContentItem` with `content_type='person'`.

**Files**:
- `crawler/douban/spiders/douban_person.py`

**Validation**:
- [ ] `scrapy crawl douban_person -a ids=1274261` scrapes person profile
- [ ] `professions` is a list (e.g., `["导演", "编剧"]`)

---

### Subtask T057 – Deduplication Pipeline

**Purpose**: Skip items already in `pending_content` with non-rejected status.

**Steps**:
1. `DeduplicationPipeline`:
   ```python
   def process_item(self, item, spider):
       with self.conn.cursor() as cur:
           cur.execute(
               "SELECT id FROM pending_content WHERE source_url = %s AND review_status != 'rejected'",
               (item['source_url'],)
           )
           if cur.fetchone():
               raise DropItem(f"Duplicate: {item['source_url']}")
       return item
   ```
2. Open DB connection in `open_spider`; close in `close_spider`.

**Files**:
- `crawler/douban/pipelines.py` (DeduplicationPipeline class)

**Validation**:
- [ ] Running spider twice for same ID: second run drops item with "Duplicate" log
- [ ] Rejected items can be re-scraped (not blocked by dedup)

---

### Subtask T058 – Normalization Pipeline

**Purpose**: Clean and normalize scraped data before DB insert.

**Steps**:
1. `NormalizationPipeline`:
   - Strip whitespace from all string fields.
   - Convert `douban_score` to `Decimal` (or None if unparseable).
   - Convert `douban_rating_count` to `int` (or None).
   - Normalize `genres` to list (deduplicate).
   - Set `submitted_at = datetime.utcnow()`.
   - Set `review_status = 'pending'`.
2. Validate required fields: `content_type` and `source_url` must be present; drop item if missing.

**Files**:
- `crawler/douban/pipelines.py` (NormalizationPipeline class)

**Validation**:
- [ ] `douban_score` stored as numeric string "8.9" → Decimal(8.9)
- [ ] Items missing `source_url` are dropped

---

### Subtask T059 – PostgreSQL Insert Pipeline

**Purpose**: Insert normalized items into `pending_content` table.

**Steps**:
1. `PostgresPipeline`:
   ```python
   def process_item(self, item, spider):
       with self.conn.cursor() as cur:
           cur.execute("""
               INSERT INTO pending_content
                   (content_type, source_url, raw_data, review_status, submitted_at)
               VALUES (%s, %s, %s::jsonb, %s, %s)
           """, (
               item['content_type'],
               item['source_url'],
               json.dumps(item['raw_data']),
               item['review_status'],
               item['submitted_at']
           ))
       self.conn.commit()
       return item
   ```
2. `raw_data` dict: all scraped fields except `content_type`, `source_url`, `review_status`, `submitted_at`.
3. Connection pooling: use single connection per spider run (not per item).

**Files**:
- `crawler/douban/pipelines.py` (PostgresPipeline class)

**Validation**:
- [ ] After crawl, `pending_content` table has new rows with `review_status='pending'`
- [ ] `raw_data` is valid JSONB with all scraped fields
- [ ] `source_url` stored for deduplication

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Douban blocks scrapers (rate limiting / CAPTCHA) | `DOWNLOAD_DELAY=2`; rotate User-Agent via middleware; use `scrapy-playwright` if JS challenge |
| HTML structure changes break selectors | Use multiple fallback selectors; log parse warnings |
| `raw_data` shape mismatch with admin approve flow | Document expected shape in `items.py`; validate in NormalizationPipeline |

## Review Guidance

- Crawler writes directly to DB (not via API) — this is intentional for performance
- `raw_data` must match `CreateMovieCommand` / `CreateTvSeriesCommand` / `CreateAnimeCommand` field names
- Dedup check: `source_url` + `review_status != 'rejected'` (rejected items can be re-scraped)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
