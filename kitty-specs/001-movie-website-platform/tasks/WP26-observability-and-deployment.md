---
work_package_id: WP26
title: Observability + Deployment
lane: planned
dependencies:
- WP02
- WP19
subtasks:
- T111
- T112
- T113
- T114
- T115
phase: Phase 6 - Ops
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

# Work Package Prompt: WP26 – Observability + Deployment

## ⚠️ IMPORTANT: Review Feedback Status

- **Has review feedback?**: Check `review_status` above.

---

## Review Feedback

*[Empty – no feedback yet.]*

---

## Implementation Command

```bash
spec-kitty implement WP26 --base WP03
```

---

## Objectives & Success Criteria

- Structured JSON logging via Serilog; request/response logging middleware
- Health check endpoint `GET /health` returns DB + Redis connectivity status
- `docker-compose.yml` runs all 4 services (api, frontend, admin, postgres, redis) locally
- GitHub Actions CI pipeline: build + test on PR; Docker image push on merge to main
- `nginx.conf` for production: reverse proxy API + serve frontend/admin static files

## Context & Constraints

- **Spec**: FR-47 (health check), FR-48 (structured logging), FR-49 (CI/CD)
- Serilog with `Serilog.Sinks.Console` (JSON format) + `Serilog.Sinks.File` (rolling daily)
- Health checks: `Microsoft.Extensions.Diagnostics.HealthChecks` + `AspNetCore.HealthChecks.NpgSql` + `AspNetCore.HealthChecks.Redis`
- Docker: multi-stage builds; API image ~200MB; frontend/admin served as static files via nginx
- CI: GitHub Actions; secrets for Docker Hub credentials and DB connection string

## Subtasks & Detailed Guidance

### Subtask T111 – Serilog Structured Logging

**Purpose**: Replace default .NET logging with Serilog JSON output.

**Steps**:
1. Install NuGet packages: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`.
2. `Program.cs`:
   ```csharp
   builder.Host.UseSerilog((ctx, cfg) => cfg
       .ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console(new JsonFormatter())
       .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));
   app.UseSerilogRequestLogging();
   ```
3. `appsettings.json` Serilog section: minimum level `Information`; override `Microsoft` to `Warning`.
4. Request logging: `UseSerilogRequestLogging()` logs method, path, status code, elapsed ms.
5. Log correlation ID: add `X-Correlation-Id` header middleware; include in log context.

**Files**:
- `api/src/API/Program.cs` (update)
- `api/src/appsettings.json` (add Serilog config)

**Validation**:
- [ ] API logs are JSON format in console output
- [ ] Each request logged with method, path, status, duration
- [ ] Log files created under `logs/` directory

---

### Subtask T112 – Health Check Endpoint

**Purpose**: `/health` endpoint reporting DB and Redis connectivity.

**Steps**:
1. Install: `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Redis`.
2. `Program.cs`:
   ```csharp
   builder.Services.AddHealthChecks()
       .AddNpgSql(connectionString, name: "postgres")
       .AddRedis(redisConnectionString, name: "redis");
   app.MapHealthChecks("/health", new HealthCheckOptions {
       ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
   });
   ```
3. Install `AspNetCore.HealthChecks.UI.Client` for JSON response format.
4. Response shape:
   ```json
   {"status":"Healthy","checks":[
     {"name":"postgres","status":"Healthy","duration":"00:00:00.012"},
     {"name":"redis","status":"Healthy","duration":"00:00:00.003"}
   ]}
   ```
5. `/health` endpoint is public (no `[Authorize]`).

**Files**:
- `api/src/API/Program.cs` (update)

**Validation**:
- [ ] `GET /health` returns 200 with both checks healthy
- [ ] With Redis down: returns 503 with redis check `Unhealthy`
- [ ] Endpoint accessible without JWT token

---

### Subtask T113 – Docker Compose (Local Dev)

**Purpose**: Single `docker-compose.yml` to run all services locally.

**Steps**:
1. `docker-compose.yml` at repo root:
   ```yaml
   services:
     postgres:
       image: postgres:15
       environment: { POSTGRES_DB: avfans, POSTGRES_PASSWORD: dev }
       ports: ["5432:5432"]
       volumes: ["pgdata:/var/lib/postgresql/data"]
     redis:
       image: redis:7-alpine
       ports: ["6379:6379"]
     api:
       build: ./api
       ports: ["5000:8080"]
       environment:
         ConnectionStrings__Default: "Host=postgres;Database=avfans;Username=postgres;Password=dev"
         Redis__ConnectionString: "redis:6379"
       depends_on: [postgres, redis]
     frontend:
       build: ./frontend
       ports: ["3000:80"]
     admin:
       build: ./admin
       ports: ["3001:80"]
   volumes:
     pgdata:
   ```
2. `api/Dockerfile`: multi-stage (sdk → runtime); `EXPOSE 8080`.
3. `frontend/Dockerfile`: `node:20-alpine` build → `nginx:alpine` serve from `/usr/share/nginx/html`.
4. `admin/Dockerfile`: same pattern as frontend.

**Files**:
- `docker-compose.yml`
- `api/Dockerfile`
- `frontend/Dockerfile`
- `admin/Dockerfile`

**Validation**:
- [ ] `docker compose up` starts all 5 services
- [ ] `GET http://localhost:5000/health` returns healthy
- [ ] Frontend accessible at `http://localhost:3000`

---

### Subtask T114 – GitHub Actions CI Pipeline

**Purpose**: Automated build and Docker image push on CI.

**Steps**:
1. `.github/workflows/ci.yml`:
   ```yaml
   on:
     push: { branches: [main] }
     pull_request: { branches: [main] }
   jobs:
     build-api:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with: { dotnet-version: '10.0.x' }
         - run: dotnet build api/src/API/API.csproj
     build-frontend:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-node@v4
           with: { node-version: '20' }
         - run: cd frontend && npm ci && npm run build
     docker-push:
       needs: [build-api, build-frontend]
       if: github.ref == 'refs/heads/main'
       runs-on: ubuntu-latest
       steps:
         - uses: docker/login-action@v3
           with: { username: ${{ secrets.DOCKER_USERNAME }}, password: ${{ secrets.DOCKER_PASSWORD }} }
         - run: docker build -t ${{ secrets.DOCKER_USERNAME }}/avfans-api:latest ./api
         - run: docker push ${{ secrets.DOCKER_USERNAME }}/avfans-api:latest
   ```
2. Secrets required: `DOCKER_USERNAME`, `DOCKER_PASSWORD`.

**Files**:
- `.github/workflows/ci.yml`

**Validation**:
- [ ] PR triggers build-api and build-frontend jobs
- [ ] Merge to main triggers docker-push job
- [ ] Build failure blocks merge (branch protection rule)

---

### Subtask T115 – Nginx Production Config

**Purpose**: Nginx reverse proxy for production deployment.

**Steps**:
1. `nginx/nginx.conf`:
   ```nginx
   server {
     listen 80;
     server_name yourdomain.com;

     # Public frontend
     location / {
       root /usr/share/nginx/html/frontend;
       try_files $uri $uri/ /index.html;
     }

     # Admin frontend
     location /admin {
       alias /usr/share/nginx/html/admin;
       try_files $uri $uri/ /admin/index.html;
     }

     # API proxy
     location /api/ {
       proxy_pass http://api:8080;
       proxy_set_header Host $host;
       proxy_set_header X-Real-IP $remote_addr;
     }

     # Gzip
     gzip on;
     gzip_types text/plain application/json application/javascript text/css;
   }
   ```
2. Add `nginx` service to `docker-compose.yml` for production profile.
3. Static asset caching: `location ~* \.(js|css|png|jpg|woff2)$ { expires 1y; add_header Cache-Control "public, immutable"; }`.

**Files**:
- `nginx/nginx.conf`

**Validation**:
- [ ] `GET /` serves frontend SPA
- [ ] `GET /admin` serves admin SPA
- [ ] `GET /api/v1/health` proxied to API container
- [ ] Static assets served with 1-year cache headers

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Serilog JSON logs hard to read in dev | Add `WriteTo.Console()` (plain text) for `Development` environment; JSON only in Production |
| Docker build slow in CI (no layer cache) | Use `actions/cache` for Docker layer cache; use `--cache-from` flag |
| Nginx SPA routing: 404 on direct URL access | `try_files $uri $uri/ /index.html` handles client-side routing |

## Review Guidance

- Health check: public endpoint (no auth); returns 503 when any check fails
- Nginx: `try_files` required for Vue Router history mode (otherwise direct URL access returns 404)
- CI: docker-push only on `main` branch merge (not on PRs)

## Activity Log

- 2026-02-21T00:00:00Z – system – lane=planned – Prompt created.
