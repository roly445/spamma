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
