# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Email capture module: `Spamma.Modules.EmailInbox`
- SMTP flow: `SmtpHostedService` (background service) → `SmtpServer` → `SpammaMessageStore.SaveAsync`
- Domain validation: `SearchSubdomainsQuery` — must return at least one `SubdomainStatus.Active` match for recipient domain
- Storage abstraction: `IMessageStoreProvider` with `StoreMessageContentAsync`, `DeleteMessageContentAsync`, `LoadMessageContentAsync`
- `LocalMessageStoreProvider` stores `.eml` files in `{ContentRootPath}/messages/{messageId}.eml`
- File system abstracted via `IDirectoryWrapper` and `IFileWrapper` for testability
- Rollback: if `ReceivedEmailCommand` fails after content stored → call `DeleteMessageContentAsync` first
- SMTP server port: 1025 (default, configurable)
- `ReceivedEmailCommand` dispatched via `ICommander` after successful domain validation and content storage
- SMTP response codes: `SmtpResponse.Ok` (success), `SmtpResponse.MailboxNameNotAllowed` (no valid domain), `SmtpResponse.TransactionFailed` (storage/command failure)
- **Storage order matters**: content must be stored via `IMessageStoreProvider` BEFORE dispatching the command, not after — this enables rollback on command failure
- **`BackgroundTaskService` DI pattern**: resolve `IMessageStoreProvider` (and `ICommander`) from a single scope created at service startup; pass both as parameters into `ExtractEmailAddressesAndSendCommand`

## Learnings — 2026-04-21 (Full Pipeline Review)

### Architecture (Actual vs Charter)
The pipeline has been significantly refactored. `SpammaMessageStore` no longer calls `IMessageStoreProvider` or `ICommander` directly. Instead it validates domain via `ISubdomainCache` (Redis + `SearchSubdomainsQuery` fallback), handles chaos addresses via `IChaosAddressCache`, then enqueues work to `IBackgroundTaskQueue`. Actual file writing and command dispatch happen in `BackgroundTaskService`.

### Critical Bugs Found

- **`IMessageStoreProvider` bypassed**: `BackgroundTaskService.ExtractEmailAddressesAndSendCommand` writes `.eml` files directly (`Directory.CreateDirectory` + `message.WriteToAsync()`), completely bypassing `LocalMessageStoreProvider`. `IMessageStoreProvider` is only used by `DeleteFileWhenEmailIsDeleted`.
- **No rollback logic**: `BackgroundTaskService` dispatches `ReceivedEmailCommand` first, then writes the file. The command result is never checked — if the command fails, the file is still written. No cleanup of stored content on command failure.
- **Single DI scope for all background jobs**: `BackgroundTaskService` creates one DI scope at startup and reuses it for all work items. Scoped services like `IDocumentSession` (Marten) are shared across all email processings — risk of dirty state and unit-of-work collisions.
- **Wrong sort key in `SubdomainCache`**: `SearchSubdomainsQuery` passes `SortBy: "domainname"` but `SearchSubdomainsQueryProcessor` only handles `"subdomainname"` and `"parentdomainname"` — falls to `default` (CreatedAt sort), meaning results are not sorted by domain name. With `Contains` search and wrong sort, could return wrong subdomain.

### Warnings Found

- **`SubdomainStatus.Inactive` accepted**: `SubdomainCache` only blocks `SubdomainStatus.Suspended`. `Inactive` subdomains pass through and accept email. Should also block `Inactive`.
- **Only `To:` header validated for domain**: `SpammaMessageStore` only parses `To:` recipients for subdomain lookup. Emails where the Spamma address is only in `Cc:` or `Bcc:` are rejected.
- **Silent exception swallowing**: `BackgroundTaskService` `catch (Exception) { // Log the exception if necessary }` — no actual logging.
- **`SmtpResponse.TransactionFailed` never used**: All processing is async after `Ok` is returned. SMTP senders never learn of processing failures.
- **SMTP port hardcoded to 25**: No configuration from appsettings. Port 25 only, no config override.

### Minor Issues

- `LocalMessageStoreProvider.LoadMessageContentAsync` has wrong error log message: "Failed to create directory" should be "Failed to load message content".
- `SpammaMessageStore` uses `DateTimeOffset.Now` directly (not `TimeProvider`).
- `EmailCleanupBackgroundService` checks `if (result != null)` instead of `result.Status == CommandResultStatus.Succeeded`.

### What Is Working Well

- `SmtpHostedService` is clean and correct; `CancellationToken` properly propagated.
- `SearchSubdomainsQuery` decorated with `[BluQubeQuery(Path = "api/subdomains/search")]` ✅
- `IDirectoryWrapper` / `IFileWrapper` abstractions exist in `Spamma.Modules.Common` and are registered in `Program.cs` ✅
- Domain validation falls back gracefully to database via `SearchSubdomainsQuery` when Redis cache misses.
- `SearchSubdomainsQueryProcessor` handles unauthenticated callers (SMTP context) by returning all subdomains when no HTTP context present (no domain access filter added for unauthenticated users).
- Chaos address handling correctly returns configurable SMTP response codes.

## Learnings — Email Viewer Component (2026-04-21)

### Blazor WebAssembly Email Rendering
- Email viewer component: `Spamma.App.Client/Components/UserControls/EmailViewer.razor` + `.razor.cs`
- Renders email HTML content in iframe with `srcdoc` attribute for security isolation
- Sandbox attributes prevent email scripts from accessing parent page
- `PrepareHtmlForIframe()` injects `<base target="_blank">` to force all links to open in new tabs
- Iframe requires `sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"` for links to work
- Static helper methods (like `PrepareHtmlForIframe`) must be placed before instance members per StyleCop SA1204

## Learnings — Catch-All SMTP Assessment (2026-04-21)

### SmtpServer 11.0.0 Hooks
- `IMailboxFilter` interface has `CanDeliverToAsync(ISessionContext, IMailbox to, IMailbox from, CancellationToken) → Task<bool>` — fires at RCPT TO stage
- No `IMailboxFilter` is currently registered → default `MailboxFilter` base class accepts all recipients already
- SmtpServer resolves `IMailboxFilter` via the ASP.NET Core `IServiceProvider` passed to its constructor → register with `services.AddTransient<IMailboxFilter, ...>()`
- `ISessionContext.EndpointDefinition.Endpoint` is `IPEndPoint` → `.Port` is accessible in both filter and message store — enables port-based catch-all differentiation
- Multiple endpoints are trivially configured: multiple `.Endpoint(builder => builder.Port(...))` calls on `SmtpServerOptionsBuilder`

### Catch-All Implementation Design
- Catch-all rejection currently happens at **application level** in `SpammaMessageStore.SaveAsync`, not SMTP protocol level
- Minimal change: check catch-all flag before domain validation loop, skip to `StandardEmailCaptureJob` with sentinel IDs
- Best virtual domain strategy: seeded system domain with well-known GUID constants (Option B) — maintains referential integrity and authorization model
- Dual-port approach (port 25 strict, port 1026 catch-all) is clean and viable — port check in `SaveAsync` via `context.EndpointDefinition.Endpoint.Port`
- Assessment written to: `C:\Code\spamma\.squad\decisions\inbox\foehammer-catchall-smtp-assessment.md`

## Learnings — Catch-All Port 1026 Implementation (2026-04-21)

### What Was Implemented
- Dual-port catch-all SMTP listener: port 1025 (strict) + optional port 1026 (catch-all)
- `EmailInboxSettings` added to `Infrastructure/Settings/` with `Port`, `CatchAllPort`, `CatchAllPortEnabled`
- Registered via `builder.Services.Configure<EmailInboxSettings>` on `"SmtpServer"` config section
- `CatchAllConstants` in `Infrastructure/Constants/` with distinct DomainId + SubdomainId GUIDs
- `SpammaMessageStore` checks `context.EndpointDefinition.Endpoint.Port` — if matches `CatchAllPort` AND `CatchAllPortEnabled`, skips domain validation and queues `StandardEmailCaptureJob` with sentinel GUIDs
- `Module.cs` conditionally adds second SmtpServer endpoint on `CatchAllPort` when enabled
- `appsettings.json` has `SmtpServer` section; `docker-compose.yml` documents port 1026 (commented)

### Pre-Existing Issues Fixed
- `EmailInboxSettingsService` was referencing old class name `EmailInboxSettings` (renamed to `EmailInboxSettingsDocument`) — fixed
- `CatchAllEmailCaptureJob.IsCatchAll` triggered SA2325 (make static) — fixed
- `CatchAllConstants` in `Infrastructure.Services` had duplicate GUID for DomainId/SubdomainId — deleted, replaced by correct version in `Infrastructure.Constants`
- `Module.cs` was missing `IEmailInboxSettingsService` registration — added

### Key Architecture Decisions
- Port-based approach: `CatchAllPortEnabled=false` → port 1026 not added as endpoint → defence in depth (even if port check triggers, `CatchAllPortEnabled=false` guard prevents bypass)
- Use `StandardEmailCaptureJob` with sentinel GUIDs instead of a separate `CatchAllEmailCaptureJob` for catch-all path — simpler, `BackgroundTaskService` doesn't need changes
- `EmailInboxSettings.Port` defaults to 25 in C# class; set to 1025 in `appsettings.json` for development/production parity

### TDD Results
- 9 `SpammaMessageStore` tests passing (4 existing + 2 new catch-all port + 3 port-boundary scenarios)
