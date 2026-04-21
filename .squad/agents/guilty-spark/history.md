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
