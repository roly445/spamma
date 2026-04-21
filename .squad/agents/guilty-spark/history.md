# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
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
