# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Blazor server project (`Spamma.App`) is ONLY for static pages. All interactive components live in `Spamma.App.Client`
- Static server pages must have `@attribute [ExcludeFromInteractiveRouting]` to prevent interactive routing
- Frontend assets: SCSS + TypeScript in `src/Spamma.App/Spamma.App/Assets/`. Compiled by webpack to `wwwroot/`
- Build command: `cd src/Spamma.App/Spamma.App && npm run build` (or `npm run watch` for dev)
- Setup pages: Welcome, Keys, Email, Admin, Hosting, Complete — all under `Components/Pages/Setup/`
- Setup TypeScript scripts: `Assets/Scripts/setup-*.ts` — handle UI logic, form submission, preset selection
- Client components use `ICommander` (for commands) and `IQuerier` (for queries) — injected via DI, no raw HTTP
- `IJSRuntime` for browser API access (WebAuthn, localStorage) — available in client WASM context
- Tailwind CSS v4 via webpack integration
