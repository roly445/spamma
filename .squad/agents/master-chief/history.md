# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis integration events), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Modular monolith under `src/modules/` — each module has `.Client` (contracts/DTOs) and server-side implementation
- Clean Architecture enforced: Domain → Application → Infrastructure, no layer leaking
- One type per file (SA1649 enforced). CQRS split into separate files: Query, QueryResult, data models, QueryProcessor
- `[BluQubeQuery(Path = "...")]` required on WASM queries; `[BluQubeCommand(Path = "...")]` on WASM commands
- StyleCop is active. SA1600 (XML docs) is suppressed — do NOT add `/// <summary>` comments
- SA1309 suppressed — underscore prefix `_fieldName` IS the convention for private fields
- All modules expose static `Module` class with `Add*()`, `Configure*()`, `AddJsonConvertersFor*()` extension methods
- Blazor server project (`Spamma.App`) handles only static pages (Login, Setup, Error) — mark with `[ExcludeFromInteractiveRouting]`
- All interactive UI lives in `Spamma.App.Client` Blazor WASM project
- Authentication: cookie-based (`SpammaAuth`), HttpOnly, issued server-side after magic link or passkey validation
