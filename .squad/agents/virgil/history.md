# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- SA1600 is suppressed — no `/// <summary>` XML doc comments anywhere
- Code should be self-documenting through clear naming conventions
- Module structure: each module under `src/modules/` with a `.Client` sub-project for contracts/DTOs
- Key quickstart: `docker-compose up -d` → `dotnet run --project src/Spamma.App/Spamma.App` → `npm run build` for frontend assets
- Key config file: `appsettings.json` in `Spamma.App` — connection strings point to Docker services by default
- Setup flow: Welcome → Keys → Email → Admin → Hosting → Complete (all static Razor pages)
