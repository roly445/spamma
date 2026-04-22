# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

- Catch-all mode implementation (2026-04-21): Settings stored in `EmailInboxSettingsDocument` (Marten document, Id = `00000000-0000-0000-0000-000000000002`). Sentinel GUIDs: `CatchAllConstants.DomainId` = `CatchAllConstants.SubdomainId` = `00000000-0000-0000-0000-000000000001`. Design chose flag-based (not port-based) approach per master-chief decision.
- `SpammaMessageStore` resolves `IEmailInboxSettingsService` from the DI scope (not constructor-injected) since `MessageStore` base class manages its own DI scope. Use `scope.ServiceProvider.GetRequiredService<T>()` for any services needed conditionally.
- `CatchAllEmailCaptureJob` is a dedicated background job type for catch-all emails; `BackgroundTaskService` handles it by calling `ExtractEmailAddressesAndSendCommand` with `isCatchAll: true`.
- `SearchEmailsQueryProcessor` now includes catch-all emails (sentinel SubdomainId) for users with `SystemRole.DomainManagement` flag — no ViewableSubdomains membership required.
- `EmailInboxSettingsDocument` seeded at startup in `Program.cs` (before middleware pipeline) to ensure the settings document exists in Marten with default `CatchAllModeEnabled = false`.
- `UpdateCatchAllModeCommand` is a WASM-facing command (`[BluQubeCommand]`) requiring `MustBeAuthenticatedRequirement`. Reads/writes via `IEmailInboxSettingsService` (scoped).
- When writing test for `SpammaMessageStore` rejection path: must register `IEmailInboxSettingsService` returning `false` in the service provider — `SpammaMessageStore` calls `GetRequiredService<IEmailInboxSettingsService>()` only when no valid subdomain found.

- Health checks (2026-04-21): Added `AspNetCore.HealthChecks.NpgSql` and `AspNetCore.HealthChecks.Redis` (both v9.0.0) — latest stable, multi-targets net9.0, compatible with net10.0. Connection string keys are `DefaultConnection` (PostgreSQL) and `Redis`. Endpoint mapped at `/health`. No TDD cycle needed for infrastructure wiring like this; confirm via build + smoke-test against running infra.
- `dotnet build --no-restore` will succeed even without new packages downloaded if the assemblies are already cached from the implicit restore that ran before; always run the full `dotnet build` (with restore) after adding NuGet packages to confirm packages resolve correctly.
- .NET 10 upgrade (2026-04-21): All project TFMs were already on `net10.0` and `global.json` already targeted SDK `10.0.0`. The only work needed was updating 7 Microsoft.AspNetCore.*/Microsoft.Extensions.* packages from `9.0.x` to `10.0.6` in `Spamma.App.csproj`, `Spamma.App.Client.csproj`, and `Spamma.App.Tests.csproj`. No code changes required; build remained at 0 errors/0 warnings throughout.
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
- Fix (2026-04-21): `PasskeyAuthenticated` event now carries `UserId`; `UserLookupProjection` patches by `@event.Data.UserId`; `Passkey.RecordAuthentication` passes `this.UserId` into the event
- Fix (2026-04-21): `AuthenticateWithPasskeyCommandHandler` user-not-found path now returns `CommonErrorCodes.NotFound` instead of `UserManagementErrorCodes.AccountSuspended`
- Fix (2026-04-21): `gat-by-id` typo corrected to `get-by-id` in both `GetDetailedDomainByIdQuery` and `GetDetailedSubdomainByIdQuery` `[BluQubeQuery]` path attributes
- CAP assembly registration (2026-04-21): DomainManagement.Module assembly was missing from CAP `.AddSubscriberAssembly()` call in Program.cs; added alongside UserManagement, EmailInbox, and Program assemblies
- Projection boundary violations (2026-04-21): DomainManagement projections were directly referencing and patching `UserManagement.Infrastructure.ReadModels.UserLookup` (cross-module infrastructure dependency); fixed by:
  * Removing UserManagement reference from DomainManagement.csproj (added UserManagement.Client for queries only)
  * DomainManagement projections now only update their own read models (counters only)
  * Integration events enhanced to carry UserName/UserEmail (UserAddedAsDomainModerator, UserAddedAsSubdomainModerator, UserAddedAsSubdomainViewer)
  * CAP subscribers in each module patch their own read models:
    - DomainManagement: DomainModeratorListEventHandler, SubdomainModeratorListEventHandler
    - UserManagement: UserDomainMembershipEventHandler
  * Command handlers now query UserManagement for user details before publishing events (via IQuerier)
- `QueryResultStatus` enum is in `BluQube.Constants` namespace, not `BluQube.Queries` — need `using BluQube.Constants;` to use it

- `CatchAllSenderAddressLookup` read model + `CatchAllSenderAddressLookupProjection` (2026-04-22): Added `Infrastructure/ReadModels/CatchAllSenderAddressLookup.cs` (Id, SenderAddress, AssignedUserIds, IsRemoved, AddedAt) and `Infrastructure/Projections/CatchAllSenderAddressLookupProjection.cs`. Used `EventProjection` base class (not `SingleStreamProjection<T>`) because Marten 8.13.3 does not expose a single-generic `SingleStreamProjection<T>` — only `SingleStreamProjection` (non-generic, in `Marten.Events.Aggregation`) and `EventProjection`. All existing projections in this codebase use `EventProjection`; matched that pattern. `UserAssignedToCatchAllSender` uses `.Append()`, `UserUnassignedFromCatchAllSender` uses `.Remove()`, `CatchAllSenderAddressRemoved` uses `.Set(IsRemoved, true)`. Registered in `Module.ConfigureEmailInbox` with `ProjectionLifecycle.Inline` + `Schema.For<CatchAllSenderAddressLookup>().Identity(x => x.Id)`. Build: 0 errors, 0 warnings.
- `CatchAllSenderAddress.Events.cs` uses the `using` import style (like `Email.Events.cs`), not the `Events.` prefix style (like `Campaign.Events.cs`). Both compile — prefer the import style for brevity.
- `CatchAllSender` client commands (2026-04-22): Added 4 WASM-facing command contracts under `Spamma.Modules.EmailInbox.Client/Application/Commands/CatchAllSender/`: `AddCatchAllSenderAddressCommand`, `RemoveCatchAllSenderAddressCommand`, `AssignUserToCatchAllSenderCommand`, `UnassignUserFromCatchAllSenderCommand`. All decorated with `[BluQubeCommand]` and following `api/email-inbox/catch-all-senders/*` path convention. Build: 0 errors, 0 warnings.

- Catch-all query processors (2026-04-22): Created `SearchCatchAllSenderAddressesQueryProcessor` (paginated, ordered by `AddedAt` desc, no user filter — admin-only via authorizer), `GetCatchAllSenderAddressDetailQueryProcessor` (load by Id, `Failed()` if null). Both authorizers use `MustBeAuthenticatedRequirement` — no `EmailInbox` `SystemRole` flag exists in the codebase; existing EmailInbox authorizers all use authenticated-only. Updated `GetCatchAllEmailsQueryProcessor`: injects `IHttpContextAccessor`, checks `SystemRole.DomainManagement` flag (same pattern as `SearchEmailsQueryProcessor`); non-admins filtered to emails whose `CatchAllSenderAddressId` is in the set of sender addresses assigned to the user; grouping changed from domain suffix to full From address. Build: 0 errors, 0 warnings.

- CatchAllSender command handlers (2026-04-22): Created `ICatchAllSenderAddressRepository` (extends `IRepository<CatchAllSenderAddress>`) + `CatchAllSenderAddressRepository` (extends `GenericRepository<T>`). Added 4 handlers (`Add`, `Remove`, `AssignUser`, `UnassignUser`), 4 validators, 4 authorizers all using `MustBeAuthenticatedRequirement`. Registered repository as scoped in `Module.AddEmailInbox()`. Build: 0 errors, 0 warnings. No role-based requirement available in Common — authorizers match `UpdateCatchAllModeCommandAuthorizer` pattern exactly. `AssignUser`/`UnassignUser` handlers do not inject `TimeProvider` (domain methods take no timestamp).

## Cross-Agent Dependencies (2026-04-21 Session)

**cortana-healthchecks** ↔ **guilty-spark-dashboard**:
- Health checks provide `/health` endpoint monitored by Aspire Dashboard in docker-compose.yml
- Both agents coordinated on observability infrastructure

**cortana-cap-boundary** ↔ **arbiter-domain-tests**:
- CAP subscriber assembly fix ensures integration events fire for DomainManagement cache invalidation
- arbiter verified fixes via real SpammaMessageStore tests that depend on working CAP event flow

## Task: EmailInbox.Client catch-all sender query contracts (2026-04-22)

- Created `SearchCatchAllSenderAddressesQuery.cs` — `[BluQubeQuery(Path = "api/email-inbox/catch-all-senders")]`, paged (Page/PageSize defaults 1/50)
- Created `SearchCatchAllSenderAddressesQueryResult.cs` — nested `SenderAddressSummary` record with Id, SenderAddress, AssignedUserCount, IsRemoved, AddedAt
- Created `GetCatchAllSenderAddressDetailQuery.cs` — `[BluQubeQuery(Path = "api/email-inbox/catch-all-senders/detail")]`, takes `SenderAddressId` Guid
- Created `GetCatchAllSenderAddressDetailQueryResult.cs` — flat result with AssignedUserIds list
- Updated `GetCatchAllEmailsQueryResult.cs`: renamed `DomainGroup(string Domain, ...)` → `SenderGroup(string SenderAddress, ...)` to reflect grouping by exact sender address
- `Spamma.Modules.EmailInbox.Client` builds 0 errors / 0 warnings
- Downstream compile errors expected in `GetCatchAllEmailsQueryProcessor` and the CatchAll Blazor component (reference `DomainGroup`/`.Domain`) — flagged for fix by other tasks

## Task: ICatchAllSenderAddressCache + CatchAllSenderAddressCache (2026-04-22)

- Created `Infrastructure/Services/Caching/ICatchAllSenderAddressCache.cs` with nested `CachedSenderAddress(Guid SenderAddressId, string SenderAddress, IReadOnlyList<Guid> AssignedUserIds)` record. Returns nullable (not Maybe<>) per task spec — differs from ISubdomainCache / IChaosAddressCache which use Maybe<>.
- Created `Infrastructure/Services/Caching/CatchAllSenderAddressCache.cs`: Redis-backed (`IConnectionMultiplexer` → `IDatabase`), `IQuerySession` for direct Marten queries against `CatchAllSenderAddressLookup`. Cache key: `catchall-sender:{normalizedAddress}`. TTL 5 min; null sentinel (`"null"` string) cached for 1 min on miss to suppress DB hammering.
- Added `StackExchange.Redis` v2.9.32 explicitly to `Spamma.Modules.EmailInbox.csproj` (was only transitive via UserManagement).
- Registered `ICatchAllSenderAddressCache` → `CatchAllSenderAddressCache` as scoped in `Module.AddEmailInbox()`.
- Build: 0 errors, 0 warnings.

## Task: Auth/magic-link email flow logging audit (2026-04-22)

- Audited full magic link flow: `StartAuthenticationCommandHandler` → `AuthenticationStartedIntegrationEvent` → `SendAuthenticationEmailToUser` → `EmailSender` (FluentEmail/SmtpClient).
- **Root causes of silent failures found:**
  1. `SendAuthenticationEmailToUser` had no `ILogger<T>`, and `if (token.IsFailure) { return; }` was completely silent — token generation failures (e.g. missing `SigningKeyBase64`) would kill the flow with no trace.
  2. `await emailSender.SendEmailAsync(...)` result was discarded — SMTP send failures were invisible.
  3. `AuthTokenProvider.ProcessToken` had bare `catch` with no logging — JWT validation exceptions silently became `Result.Fail`.
  4. `StartAuthenticationCommandHandler` and `CompleteAuthenticationCommandHandler` had zero structured logging.
  5. `EmailSender` had no logger — `sendResponse.ErrorMessages` never surfaced.
  6. `SendWelcomeEmailToNewUsers` returned the `Task<Result>` without checking the result.
- **Fixed:** Added structured `ILogger<T>` logging at every step: entry, success, warning (not-found/skipped), error (with exception) at all catch/failure paths; `SendEmailAsync` result now checked with explicit `LogError`; `AuthTokenProvider` catch logs `LogWarning(ex, ...)`.
- Updated tests: `SendAuthenticationEmailToUserTests` constructor reordered (logger first); `AuthTokenProviderTests` already had logger mock — no change needed.
- Build: 0 errors, 0 warnings.
- **Primary diagnosis:** `Settings.SigningKeyBase64` missing/empty causes token generation to throw, which was previously swallowed. Secondary: SMTP port mismatch (dev config uses 2025, Docker MailHog default is 1025).

