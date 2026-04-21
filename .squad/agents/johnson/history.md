# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- 2026-04-21: setup script bug fixes -->
- `setup-admin.ts` was exporting `SetupAdmin` class but never instantiating it — always add `new ClassName()` at EOF (matching the pattern in `setup-keys.ts`)
- `setup-email.ts` preset selectors used `[onclick="..."]` which never matched — Email.razor uses `data-preset` attributes; always verify selector strategy against the actual Razor HTML
- `npm run build` fails if `node_modules` not present — run `npm install` first; webpack is not in PATH by default on this machine

<!-- Append new learnings below. Each entry is something lasting about the project. -->

<!-- 2026-04-21: autocomplete overflow fix -->
- **ModalBase has overflow-hidden by default** — this clips absolutely-positioned children (dropdowns, tooltips). When adding any component with a `position: absolute` dropdown inside a modal, check ModalBase first.
- **bUnit 2.x API changed**: `TestContext` → `BunitContext`, `RenderComponent<T>` → `Render<T>`. Always check the bUnit migration docs when adding component tests.
- **bUnit 2.7.2 works on net10.0** — install with `dotnet add package bunit --prerelease` from `tests/Spamma.App.Tests/`.
- **overflow-hidden on rounded modal containers** — Tailwind's `rounded-lg` does NOT require `overflow-hidden` to clip corners when child content supplies its own rounded wrapper. Removing `overflow-hidden` from the outer container is safe here.
- **TDD for Razor HTML structure**: Use bUnit to assert class presence/absence on rendered DOM elements — more reliable than file-content string checks.
- Blazor server project (`Spamma.App`) is ONLY for static pages. All interactive components live in `Spamma.App.Client`
- Static server pages must have `@attribute [ExcludeFromInteractiveRouting]` to prevent interactive routing
- Frontend assets: SCSS + TypeScript in `src/Spamma.App/Spamma.App/Assets/`. Compiled by webpack to `wwwroot/`
- Build command: `cd src/Spamma.App/Spamma.App && npm run build` (or `npm run watch` for dev)
- Setup pages: Welcome, Keys, Email, Admin, Hosting, Complete — all under `Components/Pages/Setup/`
- Setup TypeScript scripts: `Assets/Scripts/setup-*.ts` — handle UI logic, form submission, preset selection
- Client components use `ICommander` (for commands) and `IQuerier` (for queries) — injected via DI, no raw HTTP
- `IJSRuntime` for browser API access (WebAuthn, localStorage) — available in client WASM context
- Tailwind CSS v4 via webpack integration
- **2026-04-21 Frontend Review findings:**
  - `setup-email.ts` preset handler uses wrong selector (`[onclick="setSmtpPreset(...)"]`) — Email.razor uses `data-preset` buttons. Presets are silently broken.
  - `setup-admin.ts` exports class but never instantiates it — script runs as a no-op on /setup/admin.
  - `webpack.config.js` uses `devtool: 'inline-source-map'` globally — TypeScript source embeds in production bundles. No env split.
  - `webpack.config.js` has leftover `generator: { filename: 'abc.js' }` in ts-loader rule — harmless dead config.
  - `package.json` has dead Parcel config (`parcel-namer-rewrite`, `targets.default.distDir`) — leftover from migration to webpack.
  - `Home.razor.cs` calls StateHasChanged() redundantly after each SignalR callback that already calls LoadEmails() (which itself calls StateHasChanged).
  - `login.ts` has a large commented-out initiatePasskeyLogin() block — should be deleted.
  - `setup-admin.ts` has dead `setExample()` helper — defined, never called.
  - `setup-hosting.ts` typo: `existingHostingSctions` (missing 'e').
  - `AppLayout.razor.cs` uses `.Wait()` in Dispose() on an async method — should use IAsyncDisposable.
  - Inline styles in Keys.razor (`min-height: 300px; cursor: crosshair`) and DomainIcon.razor could be Tailwind classes.
- **2026-04-21 TypeScript Test Infrastructure:**
  - Vitest chosen over Jest (TypeScript-first, native ESM, no Babel config needed)
  - `happy-dom` environment faster than jsdom for simple DOM tests
  - Test files use `.test.ts` suffix in `Assets/Scripts/` directory
  - `vitest.config.ts` includes `globals: true` for describe/it/expect without imports
  - `tsconfig.json` must include test files in `include` array for TypeScript resolution
  - Run tests: `npm test` (run once), `npm run test:watch` (watch mode), `npm run test:ui` (UI mode)
  - All 11 tests passing: 3 for SetupAdmin, 8 for SetupEmailConfigurator
  - Tests validate DOM event wiring, preset application, Blazor binding triggers, public API methods

## Cross-Agent Dependencies (2026-04-21 Session)

**johnson-ts-tests** ↔ **arbiter-domain-tests**:
- TypeScript frontend testing (Vitest) coordinates with backend testing strategy
- Both agents implementing verification-based testing patterns for different layers
