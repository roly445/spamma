# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis integration events), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- **2026-04-21 Aspire Evaluation:** Assessed .NET Aspire for Spamma. Verdict: don't adopt full Aspire — ROI wrong for a single-process modular monolith. Recommended: (1) swap Jaeger for standalone Aspire Dashboard container (best feature, zero migration), (2) add health checks directly via AspNetCore.HealthChecks packages, (3) leave manual OTEL config as-is. Key risks identified: Aspire Redis/PostgreSQL integrations conflict with CAP and Marten's own connection management; custom SMTP server has no Aspire integration; AppHost orchestration model is designed for distributed systems, not monoliths. Decision written to `decisions/inbox/master-chief-aspire-eval-2026-04-21.md`.
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
- **2026-04-21 Architecture Review Findings:**
  - DomainManagement projections directly reference `UserManagement.Infrastructure.ReadModels` (UserLookup) — cross-module infrastructure coupling
  - EmailInbox directly references `UserManagement.Infrastructure.Services.ApiKeys.IApiKeyValidationService` — should go through Common abstraction
  - DomainManagement.csproj has hard references to both EmailInbox and UserManagement server projects — creates tight dependency graph
  - CAP subscriber registration in Program.cs is missing `DomainManagement.Module` assembly — `CacheInvalidationEventHandler` subscribers may not be discovered
  - `IAuthTokenProvider.cs` in Common has 3 public types in one file (ErrorCodes enum, IAuthTokenProvider interface, AuthTokenProvider class) — SA1649 violation
  - `IEmailSender.cs` in Common has 2 public types (EmailTemplateSection enum + IEmailSender interface) — SA1649 violation
  - `IInternalQueryStore.cs` in Common has 2 public types (interface + class) — SA1649 violation
  - `StartAuthenticationCommand` and `CompleteAuthenticationCommand` missing `[BluQubeCommand]` — intentional (server-only auth flow, not WASM-routed)
  - `GetUserByIdQuery` missing `[BluQubeQuery]` — likely intentional (admin server-only query)
  - `ReceivedEmailCommand` and `CampaignEmailReceivedCommand` missing `[BluQubeCommand]` — intentional (internal SMTP pipeline, not WASM-routed)
  - `GetDetailedDomainByIdQuery` and `GetDetailedSubdomainByIdQuery` have typo in path: `"gat-by-id"` should be `"get-by-id"`
  - `Settings.cs` in Common mixes auth settings with domain-specific settings (MailServerHostname, MxPriority)
  - EmailRepository directly accesses filesystem (`File.Exists`, `File.OpenRead`) instead of delegating to `IMessageStoreProvider`
  - EmailInbox.csproj has duplicate `Common.Client` ProjectReference (lines 47 and 71)
  - Domain layer across all modules is clean — zero infrastructure leaking
  - CQRS handler patterns are consistent across all 3 modules
  - Integration events properly decoupled via CAP with Redis — no direct cross-module command/query calls
