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


## Catch-All Inbox UI (2026-04-21 Session)

**Task**: Build the Catch-All Inbox UI per design decision from Master Chief.

### What was built
- **CatchAllInbox.razor** (/inbox/catch-all) — amber-accented inbox page grouping catch-all emails by recipient domain
- **AppSettings.razor** (/admin/settings) — admin settings page with catch-all toggle (enable/disable) + warning banner
- **AppLayout nav** — "Catch-All" top-bar link shown when CatchAllModeEnabled = true; "Settings" entry in administration dropdown
- **GetCatchAllEmailsQuery** + **GetCatchAllEmailsQueryResult** stubs in EmailInbox.Client with [BluQubeQuery] attribute
- **CatchAllModeEnabled** added to Spamma.Modules.Common.Settings and Spamma.App.Client.Infrastructure.Contracts.Settings
- **dynamicsettings.json** endpoint updated to expose CatchAllModeEnabled to WASM client
- **3 bUnit tests** (TDD) in Spamma.App.Tests/CatchAllInboxTests.cs — disabled state, empty state, grouped emails
- Fixed backend SonarQube S2325 on CatchAllEmailCaptureJob (removed dead static property)

### Key design choices
- Amber/yellow as catch-all accent colour — visually distinct from blue domain inboxes
- AppSettings class name used instead of Settings to avoid namespace conflict with config class in same namespace
- TDD: wrote tests first (bUnit 2.x with BunitContext / Render<T>), all 3 green
- QueryResult<T>.Succeeded(data) factory method (not object initializer — API uses primary ctor with Maybe<T>)

### Build result
- dotnet build Spamma.sln — 0 errors, 0 warnings
- dotnet test filter CatchAllInbox — 3/3 passed

## Catch-All Senders Admin Page (2026-04-21 Session)

**Task**: Build `/admin/catch-all-senders` admin page per Andrew Davis request.

### What was built
- **CatchAllSenders.razor** (`/admin/catch-all-senders`) — amber-accented admin page for managing catch-all sender addresses
- **CatchAllSenders.razor.cs** — code-behind with full state management and CQRS wiring
- **AppLayout.razor** — added "Catch-All Senders" nav link (amber filter icon) in the Administration dropdown section, before Settings

### Key design choices
- Amber colour scheme throughout to match the catch-all inbox theme
- Expandable table rows (click row → user assignment panel appears inline below the row)
- Removed addresses show with `opacity-50 line-through` and no Remove button; row click is disabled
- `@onclick:stopPropagation="true"` on Remove button to prevent row expansion toggling when clicking Remove
- Add modal is inline conditional (`@if (_showAddModal)`) — not a separate component file, per task spec
- User assignment panel shows raw GUIDs for now — user name lookup is a follow-up task
- Inline validation in `HandleAddAddress` — empty check + `@` presence check before sending command
- Fixed pre-existing build error: `CatchAllInbox.razor.cs` referenced `DomainGroup` / `group.Domain` but Cortana renamed the nested type to `SenderGroup` / `SenderAddress` — updated both the .cs and .razor files

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings

## Route Change: /app → /inbox (2026-04-21 Session)

**Task**: Change the home/landing page route from `/app` to `/inbox`.

### What was changed
- **Home.razor** (`@page "/app"` → `@page "/inbox"`) — the main inbox page in `Spamma.App.Client/Pages/`
- **AppLayout.razor** — logo/brand link `href="/app"` → `href="/inbox"`
- **Index.razor** (server landing page) — "Open App" link `href="/app"` → `href="/inbox"`
- **AuthenticationEndpoints.cs** — post-magic-link-login redirect `url = "/app"` → `url = "/inbox"`
- **VerifyLogin.razor.cs** — post-passkey-login `NavigateTo("/app")` → `NavigateTo("/inbox")`

### What was NOT changed
- `Program.cs` `/app/certs/keys` — file system path, not a route

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings
- dotnet build Spamma.App.csproj --no-restore — 0 errors, 0 warnings

## Route Rename: /app → /inbox (2026-04-21 Session)

**Task**: Change the main home/app page route from `/app` to `/inbox`.

### Files changed
- `Spamma.App.Client/Pages/Home.razor` — `@page "/app"` → `@page "/inbox"`
- `Spamma.App.Client/Layout/AppLayout.razor` — logo `href="/app"` → `href="/inbox"`
- `Spamma.App/Components/Pages/Auth/VerifyLogin.razor.cs` — `NavigateTo("/app")` → `NavigateTo("/inbox")`
- `Spamma.App/Components/Pages/Index.razor` — "Open App" button `href="/app"` → `href="/inbox"`
- `Spamma.App/Infrastructure/Endpoints/AuthenticationEndpoints.cs` — passkey post-auth redirect `url = "/app"` → `url = "/inbox"`

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings
- dotnet build Spamma.App.csproj --no-restore — 0 errors, 0 warnings

## CatchAllSenders UI Fix (2026-04-21 Session)

**Task**: Fix two UI issues on `/admin/catch-all-senders` reported by Andrew Davis.

### What was changed
- **CatchAllSenders.razor** — added `<div class="px-6 py-4 border-b border-gray-200">` header strip ("Sender Addresses") as first child of white card, matching Domains.razor table card pattern
- **CatchAllSenders.razor** — added `group` class to each `<tr>`, wrapped the Remove button in `<div class="opacity-0 group-hover:opacity-100 transition-opacity">` so it only appears on row hover

### Key design choices
- Padding fix follows Domains.razor: table cards have a padded header strip (`px-6 py-4 border-b`), not `p-6` on the card itself (which would double-pad table cells)
- Hover pattern uses CSS `group`/`group-hover` (Tailwind) — no existing codebase precedent for group-hover, but chosen over C# state (`_selectedItemId`) to keep Remove independent of expand/collapse row state
- "Add Address" button in AdminHeader.Controls left always visible (primary action, not a per-row button)
- Unassign button inside expanded detail row unaffected — it only renders when a row is selected (already contextual)

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings

## Align CatchAllSenders Page to Admin Page Style Standard (2026-04-21 Session)

**Task**: Rewrite layout skeleton of `/admin/catch-all-senders` to match established pattern from `admin/users` and `admin/domains`.

### Architecture changes (unified admin page pattern)
1. **Outer wrapper:** `<div class="min-h-screen bg-gray-50">` containing `<AdminHeader>` (was: header outside wrapper)
2. **Content container:** `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8` (was: `max-w-5xl py-6`)
3. **Search panel:** White card `bg-white rounded-lg shadow-sm border p-6 mb-6` with text input bound via `oninput` (was: no search)
4. **Table card header:** Upgraded from `h3 text-sm font-medium text-gray-700` to `text-lg font-medium text-gray-900`
5. **Loading state:** Replaced `border-b-2` border spinner with standard SVG animated spinner
6. **Table overflow:** Wrapped in `<div class="overflow-x-auto">`
7. **Empty state:** Replaced large centered card-with-icon with `text-center py-12` SVG + text pattern
8. **Table binding:** Changed from `@foreach (var item in _items)` to `@foreach (var item in FilteredItems)`

### Code-behind changes (CatchAllSenders.razor.cs)
- Added `_searchTerm` string field (initialized empty)
- Added `FilteredItems` computed property: returns all items when search is empty, filters by SenderAddress (case-insensitive contains match) when search has text
- Fixed `GetRowClasses(item)` method: removed duplicate `hover:bg-amber-50` from selected state; non-selected uses `hover:bg-gray-50` per reference

### Verification
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings

## Home Route Changed from /app to /inbox (2026-04-21 Session)

**Task**: Rename main landing page route from `/app` to `/inbox` for clearer URL semantics.

### Files changed
| File | Change |
|------|--------|
| `Spamma.App.Client/Pages/Home.razor` | `@page "/app"` → `@page "/inbox"` |
| `Spamma.App.Client/Layout/AppLayout.razor` | Logo link `href="/app"` → `href="/inbox"` |
| `Spamma.App/Components/Pages/Index.razor` | "Open App" button link → `/inbox` |
| `Spamma.App/Infrastructure/Endpoints/AuthenticationEndpoints.cs` | Post-magic-link redirect → `/inbox` |
| `Spamma.App/Components/Pages/Auth/VerifyLogin.razor.cs` | Post-passkey `NavigateTo` → `/inbox` |

### Rationale
`/inbox` better describes the page purpose (email inbox view) and aligns with existing `/inbox/catch-all` sub-route.

### Note
- `Program.cs` has `/app/certs/keys` — this is a **file system path**, not a route. Left unchanged.

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings
- dotnet build Spamma.App.csproj --no-restore — 0 errors, 0 warnings

## Modal/Slideout Overlay Audit & Migration (2026-04-21 Session)

**Task**: Full audit of all `.razor` components to ensure overlay implementations use base components (`ModalBase`, `SlideoutBase`) rather than raw `fixed inset-0` patterns.

### Audit scope
- All `.razor` files under `src/Spamma.App/Spamma.App.Client/`
- All `.razor` files under `src/Spamma.App/Spamma.App/Components/`

### Audit results & decisions

**FIXED: Users.razor edit slide-out panel**
- **Before (lines 233–358):** Hand-rolled `<div class="fixed inset-0 overflow-hidden z-50">` pattern with nested backdrop and panel divs
- **After:** Migrated to `<SlideoutBase IsVisible="@showEditPanel" OnClose="CloseEditPanel" AllowBackdropClose="true" WidthClass="max-w-md" AriaLabelledBy="editUserPanelLabel">`
- **ChildContent structure:** Header (`flex-shrink-0` prevents collapse), Body (`flex-1` fills space, `overflow-y-auto`), optional Footer (`flex-shrink-0`)

**SKIPPED: Blazor error UI (`#blazor-error-ui` in MainLayout & AppLayout)**
- Reason: Blazor runtime queries by ID + data attributes (`data-nosnippet`) + CSS class selectors (`.reload`, `.dismiss`) triggered by Blazor's error handling JS
- Wrapping in `<ModalBase>` would break Blazor's error recovery mechanism
- Must remain as raw HTML

**SKIPPED: Dropdown patterns (UserTypeahead, AppLayout settings)**
- UserTypeahead uses `absolute` positioning (not `fixed`) — standard relative-to-input dropdown
- AppLayout settings uses `absolute right-0 mt-2` — same pattern
- These are not viewport overlays; no base component needed

### Pattern established going forward

| Use Case | Component |
|----------|-----------|
| Centred dialog/modal | `<ModalBase>` |
| Side panel from right | `<SlideoutBase>` (default) |
| Side panel from left | `<SlideoutBase Direction="left">` |
| Blazor framework error UI | Raw HTML only (required by framework) |
| Dropdowns/typeaheads/tooltips | Raw `absolute` positioning (no base component) |

### SlideoutBase ChildContent pattern (established)

```razor
<SlideoutBase ...>
    <!-- Header: use flex-shrink-0 to prevent collapse when body scrolls -->
    <div class="px-4 sm:px-6 pt-6 flex-shrink-0">
        <h2 id="...">Title</h2>
    </div>
    <!-- Body: use flex-1 to fill remaining space, overflow-y-auto to scroll if needed -->
    <div class="mt-6 relative flex-1 px-4 sm:px-6 pb-6 overflow-y-auto">
        Content...
    </div>
    <!-- Optional Footer: use flex-shrink-0 -->
    <div class="px-4 sm:px-6 py-4 border-t flex-shrink-0">
        Actions...
    </div>
</SlideoutBase>
```

**Important:** SlideoutBase provides the outer `flex h-full flex-col overflow-y-auto bg-white shadow-xl` — do NOT add an extra white div wrapper.

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings

## CatchAllSenders Style Alignment (2026-04-21 Session)

**Task**: Bring `/admin/catch-all-senders` fully in line with `admin/users` and `admin/domains` style.

### Gaps identified and fixed
- **AdminHeader placement**: Was outside `min-h-screen bg-gray-50` wrapper; moved inside to match Users/Domains structure
- **Content area width**: `max-w-5xl py-6` → `max-w-7xl py-8` (matches reference pages)
- **Search bar**: Added `bg-white rounded-lg shadow-sm border p-6 mb-6` search panel with `oninput`-bound text input
- **Card header**: `text-sm font-medium text-gray-700` → `text-lg font-medium text-gray-900` (matches reference)
- **Loading spinner**: Replaced simple `border-b-2` div spinner with full SVG animated spinner (matches reference)
- **Table wrapper**: Added `overflow-x-auto` div around `<table>` (matches reference)
- **Row hover**: `hover:bg-amber-50` → `hover:bg-gray-50` in `GetRowClasses`; selected rows retain `bg-amber-50` for visual distinction
- **Empty state**: Replaced large centred card-with-icon empty state with simple `text-center py-12` SVG + headings pattern (matches reference). Message is context-aware: different text when search is active
- **`_searchTerm` + `FilteredItems`**: Added `_searchTerm` field and computed `FilteredItems` property in code-behind; table iterates `FilteredItems` instead of `_items`

### Build result
- dotnet build Spamma.App.Client.csproj --no-restore — 0 errors, 0 warnings

## Modal/Slideout Overlay Audit (2026-04-21 Session)

**Task**: Audit all Razor files for raw `fixed inset-0` overlay implementations and migrate any found to `<ModalBase>` or `<SlideoutBase>`.

### Audit findings

| File | Pattern found | Status |
|------|--------------|--------|
| `MainLayout.razor` | `fixed inset-0 z-50` — `#blazor-error-ui` | **Skip** — Blazor framework-owned element |
| `AppLayout.razor` | `fixed inset-0 z-50` — `#blazor-error-ui` + `z-50` dropdown | **Skip** — framework element; dropdown is `absolute`, not an overlay |
| `UserTypeahead.razor` | `absolute z-50` | **Skip** — typeahead dropdown, not a modal/slideout overlay |
| `Users.razor` (lines 233-358) | `fixed inset-0 overflow-hidden z-50` raw slideout | **Fixed** — migrated to `<SlideoutBase>` |
| `Users.razor` — Add User modal | `<ModalBase>` | Already correct |
| `Users.razor` — Suspend modal | `<ModalBase>` | Already correct |
| `Users.razor` — Unsuspend modal | `<ConfirmModal>` | Already correct |

### Key decisions

- **`#blazor-error-ui` exemption**: These divs use `fixed inset-0 z-50` but are controlled entirely by Blazor's JS runtime via the element id, `data-nosnippet`, and `.reload`/`.dismiss` class selectors. They must stay raw.
- **Dropdown `z-50` exemption**: `absolute z-50` on `UserTypeahead` and the settings dropdown in `AppLayout` are positioned relative to their parent containers, not viewport-fixed overlays.
- **`@if` guard preserved**: Kept `@if (showEditPanel && selectedUser != null)` wrapper for null-safety on `selectedUser` inside the template.
- **SlideoutBase padding pattern**: Outer `py-6` on the removed flex container redistributed as `pt-6 flex-shrink-0` on the header div and `pb-6` on the content div.

### Build result

- `dotnet build Spamma.App.Client.csproj --no-restore -v q` — exit code 0, 0 errors, 0 warnings
