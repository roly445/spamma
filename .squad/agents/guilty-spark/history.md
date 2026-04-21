# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### Aspire Dashboard Observability (2026-04-21)

**Decision:** Replace Jaeger with standalone Aspire Dashboard for distributed tracing.

**Implementation:**
- `docker-compose.yml`: Removed Jaeger service, added Aspire Dashboard
- Dashboard image: `mcr.microsoft.com/dotnet/aspire-dashboard:latest`
- Ports: 18888 (UI), 18889 (OTLP gRPC receiver)
- Configuration: Anonymous access enabled (`DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`)
- Migration path: Zero code changes — existing OTLP exporters point to new endpoint (port 18889)
- Health check: Monitors `/health` endpoint on port 18888

**Rationale:** Lighter-weight alternative to Jaeger, fully integrated with .NET observability ecosystem. Addresses Aspire win #1 (standalone dashboard) without adopting full AppHost orchestration.

**Services after change:**
- postgres (16)
- redis (7-alpine)
- mailhog (latest)
- aspire-dashboard (latest)

**Commit:** `71b7ec7`

### .NET 10 Infrastructure Upgrade (2026-04-21)

**Scope & Changes:**
- Updated `Dockerfile.build`: SDK and runtime images from 9.0 → 10.0
- `global.json`, `Dockerfile`, `Dockerfile.runtime`: Already at 10.0
- GitHub Actions workflows (ci.yml, release.yml, pr.yml): Already configured for 10.x
- `docker-compose.yml`: No .NET version changes needed (PostgreSQL, Redis, MailHog only)

**Files Modified:**
- `src/Spamma.App/Spamma.App/Dockerfile.build`: Line 16 (`sdk:9.0` → `10.0`), Line 52 (`aspnet:9.0` → `10.0`)

**Verification:**
- Build infrastructure now consistently targets .NET 10 across all Dockerfiles
- CI/CD pipelines continue to use `dotnet-version: 10.x` (no changes needed)
- Commit: `6bca2f3` (chore: upgrade infrastructure to .NET 10)

**Key Insight:** Most infrastructure was already prepared for .NET 10; only the build Dockerfile needed updating.

- Docker Compose at repo root: services include PostgreSQL, Redis, MailHog
- Default ports: SMTP 1025, PostgreSQL 5432, Redis 6379, app 7181
- Default PostgreSQL connection: `postgresql://postgres:password@localhost:5432/spamma`
- Start infrastructure: `docker-compose up -d`
- Build command: `dotnet build Spamma.sln --no-restore`
- Test command: `dotnet test tests/ --no-restore`
- Frontend assets: `cd src/Spamma.App/Spamma.App && npm run build`
- Webpack errors: clear with `npm ci && npm run build`
- Marten auto-creates event and snapshot tables on first run
- CAP framework uses Redis for integration event message queues — set appropriate Redis eviction policies

### Security Fixes (2026-04-21)

**Secrets Sanitization:**
- `appsettings.Development.json` certificate password replaced with placeholder `<SET_VIA_ENV_OR_USER_SECRETS>`
- File now in `.gitignore` along with pattern `appsettings.*.json` to prevent future secret commits
- Connection strings still point to local dev defaults — acceptable for local dev only (Docker Compose credentials)

**NuGet CVE Resolutions:**
- Microsoft.Bcl.Memory: 9.0.0 → 10.0.6 (HIGH severity CVE fixed)
- MimeKit: 4.14.0/4.9.0 → 4.16.0 (MODERATE severity CVE fixed)
- MailKit: 4.14.1/4.9.0 → 4.16.0 (MODERATE severity CVE fixed)
- All 18 projects in solution updated via `dotnet add` commands
- Build now succeeds without vulnerability warnings (pre-existing CS0120 and webcil errors unrelated to CVE fixes)

### Infrastructure Review (2026-04-21)

**Critical Issues Found (Build Blocking):**
- NuGet package vulnerabilities: Microsoft.Bcl.Memory (HIGH), MimeKit + MailKit (MODERATE) require immediate updates
- Hardcoded certificate password in `appsettings.Development.json` exposed in source control
- Frontend assets (`wwwroot/`) not built — missing from repository

**Warnings Identified:**
- `appsettings.Development.json` should be in `.gitignore` (user-specific config)
- Kestrel binding to non-standard port/hostname `app.spamma.local:50055` — requires setup
- MailHog container missing health check in docker-compose.yml
- PostgreSQL credentials weak in docker-compose.yml (acceptable for local dev only)

**Positive Findings:**
- Docker Compose infrastructure well-configured (health checks, volumes, restart policies)
- CI/CD pipeline comprehensive (frontend + backend builds, tests, multi-arch Docker)
- Project structure and .gitignore generally sound
- Frontend build pipeline (Webpack, Tailwind v4, TypeScript) properly configured

**Remediation Status:** Findings documented in `.squad/decisions/inbox/guilty-spark-infra-review-2026-04-21.md`

## Cross-Agent Dependencies (2026-04-21 Session)

**guilty-spark-dashboard** ↔ **cortana-healthchecks**:
- Aspire Dashboard monitors `/health` endpoint provided by cortana's health checks
- Both agents coordinated on observability infrastructure stack
