---
work_package_id: WP03
title: Backend Infrastructure Services
lane: "for_review"
dependencies: [WP02]
base_branch: 001-movie-website-platform-WP02
base_commit: 144f7daf3e641e8f67ce316aa5acc55d7d736961
created_at: '2026-02-23T06:31:40.639694+00:00'
subtasks:
- T012
- T013
- T014
- T015
- T016
phase: Phase 0 - Infrastructure Foundation
assignee: ''
agent: "codex"
shell_pid: "32436"
review_status: ''
reviewed_by: ''
history:
- timestamp: '2026-02-21T00:00:00Z'
  lane: planned
  agent: system
  shell_pid: ''
  action: Prompt generated via /spec-kitty.tasks
---

# Work Package Prompt: WP03 – Backend Infrastructure Services

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP03 --base WP02
```

---

## Objectives & Success Criteria

- `IRedisCache` service operational with all cache key patterns from data-model.md
- `ITencentCosClient` interface + implementation for COS file URL resolution
- Global exception middleware returns structured JSON errors; request logging middleware logs all requests
- `GET /api/v1/health` returns 200; `GET /api/v1/admin/stats` without JWT returns 401
- Sentry error capture working in development; Prometheus `/metrics` endpoint accessible
- Swagger UI shows all endpoints with JWT bearer auth option

## Context & Constraints

- **Plan**: `kitty-specs/001-movie-website-platform/plan.md` — cache TTL table, COS key convention
- **Data Model**: `kitty-specs/001-movie-website-platform/data-model.md` — Redis key naming spec
- **Quickstart**: `kitty-specs/001-movie-website-platform/quickstart.md` — appsettings structure
- Redis TTLs: detail pages 1h, list queries 10min, rankings 24h, autocomplete 5min, home banners 10min
- COS convention: database stores object key only (e.g., `posters/xxx.jpg`); frontend appends CDN base
- OAuth 2.0: RS256, JWKS URI from config, only `/api/v1/admin/**` routes need auth

## Subtasks & Detailed Guidance

### Subtask T012 – Redis Cache Service + CacheKeys Constants

**Purpose**: Implement `IRedisCache` interface with typed get/set/delete operations and a central `CacheKeys` class for all cache key formats.

**Steps**:
1. In `src/Domain/`, define `IRedisCache`:
   ```csharp
   public interface IRedisCache
   {
       Task<T?> GetAsync<T>(string key) where T : class;
       Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class;
       Task DeleteAsync(string key);
       Task DeleteByPatternAsync(string pattern); // uses SCAN
       Task<bool> ExistsAsync(string key);
   }
   ```
2. Implement in `src/Infrastructure/Cache/RedisCache.cs` using `StackExchange.Redis`:
   ```csharp
   public class RedisCache(IConnectionMultiplexer redis) : IRedisCache
   {
       private readonly IDatabase _db = redis.GetDatabase();

       public async Task<T?> GetAsync<T>(string key) where T : class
       {
           var value = await _db.StringGetAsync(key);
           return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : null;
       }

       public async Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
           => await _db.StringSetAsync(key, JsonSerializer.Serialize(value), ttl);

       public async Task DeleteByPatternAsync(string pattern)
       {
           // Use SCAN to find matching keys, then DELETE (batch of 100)
           var server = redis.GetServer(redis.GetEndPoints().First());
           var keys = server.Keys(pattern: pattern).ToArray();
           if (keys.Length > 0)
               await _db.KeyDeleteAsync(keys);
       }
   }
   ```
3. Create `src/Application/Common/CacheKeys.cs`:
   ```csharp
   public static class CacheKeys
   {
       public static string MovieDetail(long id) => $"movie:detail:{id}";
       public static string TvDetail(long id) => $"tv:detail:{id}";
       public static string AnimeDetail(long id) => $"anime:detail:{id}";
       public static string PersonDetail(long id) => $"person:detail:{id}";
       public static string MovieList(string hash) => $"movies:list:{hash}";
       public static string TvList(string hash) => $"tv:list:{hash}";
       public static string AnimeList(string hash) => $"anime:list:{hash}";
       public static string RankingsScore(string type) => $"rankings:{type}:score";
       public static string RankingsHot(string type) => $"rankings:{type}:hot";
       public static string SearchAutocomplete(string q) => $"search:autocomplete:{Uri.EscapeDataString(q)}";
       public static string HomeBanners => "home:banners";
   }
   ```
4. Register in DI:
   ```csharp
   builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
       ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"]!));
   builder.Services.AddScoped<IRedisCache, RedisCache>();
   ```

**Files**:
- `api/src/Domain/IRedisCache.cs`
- `api/src/Infrastructure/Cache/RedisCache.cs`
- `api/src/Application/Common/CacheKeys.cs`

**Validation**:
- [ ] `SetAsync` + `GetAsync` round-trip a DTO successfully with Redis running
- [ ] `DeleteByPatternAsync("movies:list:*")` removes all matching keys

---

### Subtask T013 – Tencent COS Storage Client

**Purpose**: Implement a thin COS interface for URL resolution (primarily) — actual uploads happen via admin endpoints later.

**Steps**:
1. Define in Domain:
   ```csharp
   public interface ITencentCosClient
   {
       string GetCdnUrl(string cosKey); // Returns CDN URL for a COS key
       Task<string> UploadAsync(Stream fileStream, string cosKey, string contentType);
       Task DeleteAsync(string cosKey);
   }
   ```
2. Implement in `src/Infrastructure/Storage/TencentCosClient.cs`:
   - `GetCdnUrl`: simply `$"{_cdnBase}/{cosKey}"` — CDN base from config
   - `UploadAsync`: use `TencentCloud.COS.Model` SDK or direct HTTP with HMAC-SHA256 signing
   - `DeleteAsync`: COS object delete API
3. Create `CosUrlHelper` as a simpler static utility for use in DTOs:
   ```csharp
   public static class CosUrlHelper
   {
       private static string _cdnBase = string.Empty;
       public static void Configure(string cdnBase) => _cdnBase = cdnBase;
       public static string? ToUrl(string? cosKey) => cosKey == null ? null : $"{_cdnBase}/{cosKey}";
   }
   ```
4. Register in DI and configure CDN base at startup from `appsettings.json` `COS:CdnBase`.

**Note**: Database stores COS object keys (e.g., `posters/movie-123.jpg`). All API responses return the key as-is; the frontend uses `VITE_COS_CDN_BASE` env to construct full URLs. The backend `CosUrlHelper` is only used in admin endpoints for validation or admin-facing responses.

**Files**:
- `api/src/Domain/ITencentCosClient.cs`
- `api/src/Infrastructure/Storage/TencentCosClient.cs`
- `api/src/Application/Common/CosUrlHelper.cs`

**Validation**:
- [ ] `CosUrlHelper.ToUrl("posters/test.jpg")` returns `https://cdn.example.com/posters/test.jpg` with config set
- [ ] `GetCdnUrl(null)` returns null (no NullReferenceException)

---

### Subtask T014 – Global Middleware (Exception, Logging, CORS, Swagger)

**Purpose**: Add production-ready middleware for exception handling, request logging, cross-origin resource sharing, and API documentation.

**Steps**:
1. `GlobalExceptionMiddleware.cs`:
   ```csharp
   public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
   {
       public async Task InvokeAsync(HttpContext context)
       {
           try { await next(context); }
           catch (Exception ex)
           {
               logger.LogError(ex, "Unhandled exception");
               context.Response.StatusCode = 500;
               context.Response.ContentType = "application/json";
               await context.Response.WriteAsync(
                   JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "服务器内部错误" } }));
           }
       }
   }
   ```
   Handle specific exception types: `KeyNotFoundException` → 404, `UnauthorizedAccessException` → 401, `ValidationException` → 422 with field errors.
2. `RequestLoggingMiddleware.cs`: log method, path, status code, and duration in milliseconds on response completion.
3. CORS policy in Program.cs:
   ```csharp
   builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
       p.WithOrigins("http://localhost:5173", "http://localhost:5174")
        .AllowAnyMethod().AllowAnyHeader()));
   ```
   In production, configure allowed origins from `appsettings.json`.
4. Swagger: `AddSwaggerGen()` with JWT bearer security definition; enable `UseSwagger()` + `UseSwaggerUI()` only in Development.
5. Health check: `app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }))`.
6. Register middleware order in Program.cs: `UseGlobalExceptionMiddleware` → `UseRequestLogging` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`.

**Files**:
- `api/src/API/Middleware/GlobalExceptionMiddleware.cs`
- `api/src/API/Middleware/RequestLoggingMiddleware.cs`
- `api/src/API/Program.cs` (update)

**Validation**:
- [ ] `GET /api/v1/health` returns `{"status":"ok"}` with 200
- [ ] Throwing an unhandled exception returns `{"error":{"code":"INTERNAL_ERROR","message":"..."}}` with 500
- [ ] CORS preflight from localhost:5173 returns 200 with correct headers

---

### Subtask T015 – OAuth 2.0 JWT RS256 Authentication

**Purpose**: Secure all `/api/v1/admin/**` endpoints with JWT Bearer token validation using JWKS endpoint.

**Steps**:
1. In Program.cs, add JWT authentication:
   ```csharp
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(opt =>
       {
           opt.MetadataAddress = builder.Configuration["Auth:JwksUri"]!;
           opt.TokenValidationParameters = new TokenValidationParameters
           {
               ValidAudience = builder.Configuration["Auth:Audience"],
               ValidIssuer = builder.Configuration["Auth:Issuer"],
               ValidateLifetime = true,
               ClockSkew = TimeSpan.FromSeconds(30),
           };
       });
   builder.Services.AddAuthorization();
   ```
2. Create `AdminController` base class:
   ```csharp
   [ApiController]
   [Route("api/v1/admin")]
   [Authorize]
   public abstract class AdminControllerBase : ControllerBase { }
   ```
3. All admin controllers extend `AdminControllerBase` — they automatically inherit `[Authorize]`.
4. Public controllers extend plain `ControllerBase` without `[Authorize]`.
5. Create `appsettings.Development.json.example` with all required config keys (from quickstart.md).

**Files**:
- `api/src/API/Controllers/AdminControllerBase.cs`
- `api/src/API/appsettings.Development.json.example`

**Validation**:
- [ ] `GET /api/v1/admin/stats` without Authorization header returns 401
- [ ] With valid JWT, returns 200 (even if empty data)
- [ ] Public endpoints (`/api/v1/movies`) return 200 without any auth

---

### Subtask T016 – Sentry Integration + Prometheus Metrics

**Purpose**: Wire up error tracking (Sentry) and basic performance metrics (Prometheus) for the observability requirement (FR-47).

**Steps**:
1. Sentry in Program.cs:
   ```csharp
   builder.WebHost.UseSentry(opt =>
   {
       opt.Dsn = builder.Configuration["Sentry:Dsn"];
       opt.TracesSampleRate = 0.1;
       opt.SendDefaultPii = false;
   });
   ```
   Guard against empty DSN: Sentry SDK handles empty DSN gracefully (no-op). Test by throwing an exception in a dev endpoint and checking Sentry dashboard.
2. Prometheus metrics using `prometheus-net.AspNetCore`:
   ```csharp
   builder.Services.AddMetricServer(opt => opt.Port = 9090); // or use existing port
   app.UseHttpMetrics(); // auto-instruments HTTP requests
   app.MapMetrics("/metrics"); // expose scrape endpoint
   ```
3. Register custom metrics for key operations:
   ```csharp
   public static class AppMetrics
   {
       public static readonly Counter SearchRequests = Metrics
           .CreateCounter("moviesite_search_requests_total", "Total search requests", ["type"]);
       public static readonly Histogram CacheHitRatio = Metrics
           .CreateHistogram("moviesite_cache_hit_ratio", "Cache hit ratio per endpoint");
   }
   ```
4. Add `appsettings.json` section:
   ```json
   "Sentry": { "Dsn": "" },
   "Metrics": { "Enabled": true }
   ```

**Files**:
- `api/src/API/Program.cs` (update)
- `api/src/API/Observability/AppMetrics.cs`

**Validation**:
- [ ] `curl http://localhost:5001/metrics` returns Prometheus text format
- [ ] HTTP request duration histogram (`http_request_duration_seconds`) is present in metrics output

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| JWKS URI unreachable in development | Configure `RequireHttpsMetadata = false` for local dev; use a mock JWT issuer |
| Redis SCAN on large keyspaces is slow | Limit pattern scope; use `SCAN COUNT 100`; only call DeleteByPattern on cache flush, not per-request |
| Sentry initializing in tests causes side effects | Guard: `if (!string.IsNullOrEmpty(dsn))` — but Sentry SDK handles empty DSN as no-op already |
| Prometheus `/metrics` endpoint exposed publicly | Bind only to internal network in production; or require local access only |

## Review Guidance

- `GET /api/v1/health` → 200 `{"status":"ok"}`
- `GET /api/v1/admin/stats` without JWT → 401
- `GET /metrics` → Prometheus format with `http_request_duration_seconds` metric
- Unhandled exception → 500 with structured JSON `{"error":{"code":"...","message":"..."}}`
- `CacheKeys.MovieDetail(123)` → `"movie:detail:123"`

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
- 2026-02-23T06:31:42Z – codex – shell_pid=32436 – lane=doing – Assigned agent via workflow command
- 2026-02-23T08:00:38Z – codex – shell_pid=32436 – lane=for_review – Ready for review: implemented cache service, COS client, middleware, JWT auth, Sentry, and metrics with smoke validation
