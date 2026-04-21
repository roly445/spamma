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
