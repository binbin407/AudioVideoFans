---
work_package_id: "WP27"
subtasks:
  - "T116"
  - "T117"
  - "T118"
  - "T119"
  - "T120"
title: "Backend xUnit Tests"
phase: "Phase 8 - Testing"
lane: "planned"
assignee: ""
agent: ""
shell_pid: ""
review_status: ""
reviewed_by: ""
dependencies: ["WP02", "WP03", "WP04", "WP05", "WP10", "WP11"]
history:
  - timestamp: "2026-02-21T00:00:00Z"
    lane: "planned"
    agent: "system"
    shell_pid: ""
    action: "Created via /spec-kitty.analyze remediation (C3: constitution test coverage)"
---

# Work Package Prompt: WP27 – Backend xUnit Tests

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP27 --base WP11
```

---

## Objectives & Success Criteria

- xUnit test projects exist under `api/tests/Unit/` and `api/tests/Integration/`
- Overall backend code coverage ≥ 80% (measured via `dotnet-coverage` + Coverlet)
- Core business paths 100% covered: admin auth guard, approve/reject workflow, soft-delete
- All tests pass in CI (`dotnet test --no-build`)
- Coverage report generated as part of `dotnet test` output (Cobertura XML)

## Context & Constraints

- **Constitution**: xUnit ≥ 80% coverage; core paths (auth, data writes) 100%
- **Plan structure**: `api/tests/Unit/` for Domain + Application; `api/tests/Integration/` for Repository + API endpoints
- Use `xunit`, `Moq` (or `NSubstitute`) for unit tests; `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for integration tests
- Integration tests use a real PostgreSQL test database (separate schema or test container via `Testcontainers.PostgreSql`)
- Do NOT use `InMemoryDatabase` — SqlSugar has no EF Core InMemory provider; use Testcontainers or a shared test DB
- Coverage threshold enforced in `Directory.Build.props` or CI step: `dotnet test --collect:"XPlat Code Coverage" /p:Threshold=80`

---

## Subtasks & Detailed Guidance

### Subtask T116 – xUnit Project Setup + Coverage Configuration

**Purpose**: Initialize the two test projects with all shared infrastructure.

**Steps**:
1. Create test projects:
   ```bash
   dotnet new xunit -n AudioVideoFans.Tests.Unit -o api/tests/Unit
   dotnet new xunit -n AudioVideoFans.Tests.Integration -o api/tests/Integration
   dotnet sln api/AudioVideoFans.sln add api/tests/Unit api/tests/Integration
   ```
2. Add project references:
   - Unit: references `Domain`, `Application`
   - Integration: references `API`, `Application`, `Infrastructure`
3. Install NuGet packages:
   - Both: `coverlet.collector`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
   - Unit: `Moq` (or `NSubstitute`)
   - Integration: `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`
4. `api/tests/Integration/Fixtures/TestWebApplicationFactory.cs`:
   ```csharp
   public class TestWebApplicationFactory : WebApplicationFactory<Program>
   {
       private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
           .WithImage("postgres:15").Build();
       private readonly RedisContainer _redis = new RedisBuilder().Build();

       protected override void ConfigureWebHost(IWebHostBuilder builder)
       {
           // Override connection strings with test containers
           builder.ConfigureServices(services => {
               // Replace ISqlSugarClient registration with test DB
               // Replace IRedisCache registration with test Redis
           });
       }
       // StartAsync / DisposeAsync to manage containers
   }
   ```
5. `api/tests/Integration/Fixtures/DatabaseFixture.cs`: run all EF migrations against test DB on startup.
6. Add to `Directory.Build.props` (or each test `.csproj`):
   ```xml
   <PropertyGroup>
     <CollectCoverage>true</CollectCoverage>
     <CoverletOutputFormat>cobertura</CoverletOutputFormat>
     <Threshold>80</Threshold>
     <ThresholdType>line</ThresholdType>
   </PropertyGroup>
   ```

**Files**:
- `api/tests/Unit/AudioVideoFans.Tests.Unit.csproj`
- `api/tests/Integration/AudioVideoFans.Tests.Integration.csproj`
- `api/tests/Integration/Fixtures/TestWebApplicationFactory.cs`
- `api/tests/Integration/Fixtures/DatabaseFixture.cs`

**Validation**:
- [ ] `dotnet test api/tests/Unit` passes with no failures (empty project is fine)
- [ ] `dotnet test api/tests/Integration` passes (container spins up and DB migrates)
- [ ] `--collect:"XPlat Code Coverage"` produces `coverage.cobertura.xml`

---

### Subtask T117 – Domain Entity Unit Tests

**Purpose**: Unit-test all domain entities for business rule invariants.

**Steps**:
1. `api/tests/Unit/Domain/MovieTests.cs`:
   - `Movie_SoftDelete_SetsDeletedAt`: verify `SoftDelete()` sets `DeletedAt = DateTime.UtcNow` and leaves other fields unchanged
   - `Movie_Restore_ClearsDeletedAt`: verify `Restore()` nulls out `DeletedAt`
   - `Movie_IsDeleted_ReturnsTrueWhenDeletedAtIsSet`
2. `api/tests/Unit/Domain/PendingContentTests.cs`:
   - `PendingContent_Approve_SetsApprovedStatus`
   - `PendingContent_Reject_RequiresNonEmptyReason`: assert throws `DomainException` when reason is null/empty
   - `PendingContent_Reset_BackToPending`: verify status resets and `ReviewedAt` clears
3. `api/tests/Unit/Domain/FeaturedBannerTests.cs`:
   - `Banner_IsActive_TrueWhenWithinTimeRange`: start_at ≤ now ≤ end_at
   - `Banner_IsActive_FalseWhenExpired`
   - `Banner_IsActive_TrueWhenBothDatesNull` (permanent banner)
4. `api/tests/Unit/Domain/SimilarContentServiceTests.cs` (if domain service):
   - Test keyword overlap scoring with mock content lists

**Files**:
- `api/tests/Unit/Domain/MovieTests.cs`
- `api/tests/Unit/Domain/PendingContentTests.cs`
- `api/tests/Unit/Domain/FeaturedBannerTests.cs`

**Validation**:
- [ ] All domain tests pass
- [ ] Soft-delete tests cover all 3 entity types (Movie, TvSeries, Anime)
- [ ] `PendingContent.Reject` with empty reason throws

---

### Subtask T118 – Application Service Unit Tests

**Purpose**: Test Application-layer services with mocked repositories and cache.

**Steps**:
1. `api/tests/Unit/Application/MovieApplicationServiceTests.cs`:
   - Mock `IMovieRepository`, `IRedisCache`, `ITencentCosClient`
   - `GetMovieDetail_CacheHit_ReturnsCachedValue`: verify repository NOT called when cache returns value
   - `GetMovieDetail_CacheMiss_QueriesRepositoryAndSetsCache`
   - `CreateMovie_ValidCommand_CallsRepositorySaveAndInvalidatesListCache`
   - `SoftDeleteMovie_ExistingMovie_SetsDeletedAtAndInvalidatesCache`
2. `api/tests/Unit/Application/PendingContentServiceTests.cs`:
   - `ApproveContent_ValidId_ReturnsPrefilledDto`: verify raw_data fields mapped to correct DTO properties
   - `ApproveContent_AlreadyApproved_ThrowsInvalidOperationException`
   - `BulkApprove_ThreeIds_CallsApproveThreeTimes`
   - `BulkApprove_PartialFailure_ReturnsFailedIds`
   - `RejectContent_EmptyReason_ThrowsDomainException`
3. `api/tests/Unit/Application/SearchServiceTests.cs`:
   - `Search_ZhparserUnavailable_FallsBackToILike`
   - `Autocomplete_ShortQuery_ReturnsEmptyGrouped` (query length < 1)

**Files**:
- `api/tests/Unit/Application/MovieApplicationServiceTests.cs`
- `api/tests/Unit/Application/PendingContentServiceTests.cs`
- `api/tests/Unit/Application/SearchServiceTests.cs`

**Validation**:
- [ ] Cache hit/miss paths both covered for at least Movie and TV series
- [ ] All reject/approve business rule paths covered (these are "core paths" per constitution)
- [ ] Mocks verify `IRedisCache.Delete()` is called after mutations

---

### Subtask T119 – Repository Integration Tests

**Purpose**: Verify SqlSugar repository implementations against a real PostgreSQL instance.

**Steps**:
1. `api/tests/Integration/Repositories/MovieRepositoryTests.cs` (use `DatabaseFixture`):
   - `GetById_ExistingId_ReturnsMovie`: seed 1 movie, fetch by ID
   - `GetById_SoftDeletedId_ReturnsNullByDefault`: soft-deleted movies hidden
   - `GetById_SoftDeletedId_IncludeDeletedTrue_ReturnsMovie`
   - `List_GenreFilter_ReturnsOnlyMatchingMovies`: seed 5 movies with different genres, verify `&&` array filter
   - `List_DecadeFilter_2020s_ReturnsMoviesFrom2020To2029`
   - `SoftDelete_SetsDeletedAt_DoesNotRemoveRow`: verify row still exists in DB after soft-delete
2. `api/tests/Integration/Repositories/PendingContentRepositoryTests.cs`:
   - `List_StatusFilter_ReturnsPendingOnly`
   - `Approve_UpdatesReviewStatus`
3. `api/tests/Integration/Repositories/CacheIntegrationTests.cs` (use Redis test container):
   - `Set_Then_Get_ReturnsSameValue`
   - `Delete_Then_Get_ReturnsNull`
   - `DeletePattern_RemovesMatchingKeys`

**Files**:
- `api/tests/Integration/Repositories/MovieRepositoryTests.cs`
- `api/tests/Integration/Repositories/PendingContentRepositoryTests.cs`
- `api/tests/Integration/Repositories/CacheIntegrationTests.cs`

**Validation**:
- [ ] Array overlap filter (`genres &&`) tested end-to-end against PostgreSQL
- [ ] Soft-delete visibility rules verified at DB level
- [ ] Redis `DeletePattern` test confirms SCAN-based deletion works

---

### Subtask T120 – API Controller Integration Tests

**Purpose**: Full HTTP round-trip tests for critical API endpoints using `WebApplicationFactory`.

**Steps**:
1. `api/tests/Integration/Controllers/MoviesControllerTests.cs`:
   - `GET /api/v1/movies` returns 200 with pagination object
   - `GET /api/v1/movies/{id}` returns full MovieDetailDto (seed test data)
   - `GET /api/v1/movies/999999` returns 404
   - `POST /api/v1/admin/movies` without JWT returns 401
   - `POST /api/v1/admin/movies` with valid JWT + valid body returns 201 with new ID
   - `DELETE /api/v1/admin/movies/{id}` with valid JWT soft-deletes (row still in DB with `deleted_at`)
2. `api/tests/Integration/Controllers/AdminAuthTests.cs`:
   - `All admin endpoints require Authorization header` — parameterized test across all `/api/v1/admin/**` routes
   - `Invalid JWT returns 401`
   - `Expired JWT returns 401`
3. `api/tests/Integration/Controllers/SearchControllerTests.cs`:
   - `GET /api/v1/search?q=` with empty q returns empty results (not 500)
   - `GET /api/v1/search/autocomplete?q=星` returns grouped response

**Helper**: `TestJwtFactory.cs` — generates valid test RS256 JWT using a test key pair (do not use real credentials).

**Files**:
- `api/tests/Integration/Controllers/MoviesControllerTests.cs`
- `api/tests/Integration/Controllers/AdminAuthTests.cs`
- `api/tests/Integration/Controllers/SearchControllerTests.cs`
- `api/tests/Integration/Helpers/TestJwtFactory.cs`

**Validation**:
- [ ] Admin auth 401 test covers every admin route prefix (`/api/v1/admin/**`)
- [ ] Soft-delete verified at HTTP layer (DELETE returns 204, subsequent GET without `include_deleted` returns 404)
- [ ] All integration tests use test containers — no mocking of PostgreSQL at this layer

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Testcontainers startup slow in CI | Cache container images in CI registry; use `--no-build` flag for test runs |
| SqlSugar TEXT[] array filter not translatable | Integration test T119 will catch this immediately — fix raw SQL in ArrayFilterHelper |
| JWT test key management | Generate throwaway RSA key pair in test fixture; never use production JWKS URI |
| Coverage threshold fails on first run | Start by targeting 60%, raise to 80% incrementally as tests are added |

## Review Guidance

- **Core paths must be 100% covered**: `PendingContent.Approve`, `PendingContent.Reject` (with validation), admin 401 guard, soft-delete set/restore
- Integration tests must use real PostgreSQL (via Testcontainers) — SqlSugar array filters are untestable with InMemory stubs
- `TestJwtFactory` must produce a token that passes the same `AddJwtBearer()` configuration used in production (same algorithm, issuer, audience)
- CI step: add `dotnet test --collect:"XPlat Code Coverage" /p:Threshold=80 /p:ThresholdType=line` to `build-and-test.yml`

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Created via analyze remediation (C3).
