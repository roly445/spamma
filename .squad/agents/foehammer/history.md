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
