# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Module registration pattern: each module has a static `Module` class with `Add*()`, `Configure*()`, `AddJsonConvertersFor*()` extension methods registered in `Program.cs`
- CQRS: Commands extend `ICommand`, queries extend `IQuery<TResult>`. Handlers use `BluQube.Commands.CommandHandler<T>` and `BluQube.Queries.QueryProcessor<TQuery, TResult>` base classes
- `[BluQubeCommand(Path = "api/...")]` and `[BluQubeQuery(Path = "api/...")]` attributes are required on WASM-facing types for code generation
- Naming convention for paths: user commands → `api/user-management/*`, domain commands → `api/domain-management/*`, user queries → `api/users/*`
- `Maybe<T>` for optional values (e.g. repo lookups), `Result<T, TError>` for operations that can fail
- Inject `TimeProvider` — never call `DateTime.UtcNow` directly
- Common error codes in `Spamma.Modules.Common.Application.CommonErrorCodes`
- Integration events published via `IIntegrationEventPublisher.PublishAsync()`, subscribed with `[CapSubscribe("EventName")]`
- Event streams stored in Marten; projections in `Infrastructure/Projections/` generate read models
- `PasskeyAuthenticated` event (2026-04-21): does NOT carry `UserId` — the `UserLookupProjection` that maps it to `UserLookup.LastPasskeyAuthenticationAt` uses `@event.StreamId` (Passkey ID), which is wrong; `LastPasskeyAuthenticationAt` is never written
- `GetDetailedDomainByIdQuery` and `GetDetailedSubdomainByIdQuery` both have `gat-by-id` typo in their `[BluQubeQuery]` path (should be `get-by-id`), breaking code-generated endpoints
- `.DateTime` vs `.UtcDateTime` trap: `GetUtcNow().DateTime` returns Kind=Unspecified; always use `.UtcDateTime` — three handlers (`DeleteEmail`, `ToggleFavorite`, `VerifyDomain`) have this bug
- `ApiKey.Revoke()` throws `InvalidOperationException` instead of returning `ResultWithError<>` — inconsistent with all other aggregate methods; caller guards exist but pattern is fragile
- `ApiKey.IsExpired` uses `DateTimeOffset.UtcNow` directly in domain aggregate — breaks testability; `SearchUsersQueryProcessor` and `ApiKeyAuthenticationHandler` also use raw `DateTime.UtcNow`/`DateTimeOffset.UtcNow`
- `CampaignEmailReceivedCommandHandler` declares its logger as `ILogger<ReceivedEmailCommandHandler>` — wrong type; logs appear under the wrong category
- `ReceivedEmailCommand` and `CampaignEmailReceivedCommand` intentionally lack `[BluQubeCommand]` — they are SMTP-triggered internal commands, not WASM-facing
- Validator coverage: all command handlers have a matching `AbstractValidator<T>` — no gaps found
- CAP integration event pairs: all `[CapSubscribe]` constants match published events — no orphans found
