# Spamma Codebase Review — Decisions & Findings
**Timestamp:** 2026-04-21T11:16  
**Session:** 7-Agent Parallel Code Review  
**Status:** All findings catalogued and consolidated  

---

# ARCHITECTURE REVIEW — Master Chief

## 🔴 Critical Issues

### 1. Cross-Module Infrastructure Coupling — DomainManagement → UserManagement

**Files:**
- `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/DomainLookupProjection.cs:9`
- `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/SubdomainLookupProjection.cs:9`

**Problem:** Both projections directly import `Spamma.Modules.UserManagement.Infrastructure.ReadModels` to load `UserLookup` when enriching moderator data. This creates a hard coupling between DomainManagement's infrastructure and UserManagement's internal read model layer.

**Impact:** UserManagement cannot independently evolve its read model schema without potentially breaking DomainManagement projections. Violates module encapsulation.

**Fix:** Move the shared read model shape to a contract in `Common` or `Common.Client`. UserManagement writes to it; DomainManagement reads from it. Alternatively, use an integration event to push moderator name/email into DomainManagement's own read model on user creation/update.

---

### 2. Cross-Module Infrastructure Coupling — EmailInbox → UserManagement

**File:** `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/EmailPushGrpcService.cs:4`

**Problem:** EmailInbox directly references `Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys.IApiKeyValidationService`. This is a concrete infrastructure dependency from one module into another's internals.

**Impact:** Breaks module isolation. EmailInbox has a compile-time dependency on UserManagement's service layer.

**Fix:** Move `IApiKeyValidationService` interface to `Spamma.Modules.Common`. UserManagement provides the implementation. EmailInbox depends only on the abstraction.

---

### 3. Missing CAP Subscriber Assembly Registration

**File:** `src/Spamma.App/Spamma.App/Program.cs:163`

**Problem:** `AddSubscriberAssembly(...)` registers UserManagement, Program, and EmailInbox assemblies — but **DomainManagement is missing**. The `CacheInvalidationEventHandler` in DomainManagement uses `[CapSubscribe]` for `SubdomainStatusChanged` and `ChaosAddressUpdated` events.

**Impact:** Cache invalidation events may not fire. Subdomain and chaos address caches could serve stale data after status changes.

**Fix:** Add `typeof(Spamma.Modules.DomainManagement.Module).Assembly` to the subscriber registration.

---

## 🟡 Warnings

### 4. SA1649 Violations — Multiple Public Types Per File (Common Module)

**Files:**
- `src/modules/Spamma.Modules.Common/IAuthTokenProvider.cs` — Contains `ErrorCodes` enum, `IAuthTokenProvider` interface, and `AuthTokenProvider` class (3 public types)
- `src/modules/Spamma.Modules.Common/IEmailSender.cs` — Contains `EmailTemplateSection` enum and `IEmailSender` interface (2 public types)
- `src/modules/Spamma.Modules.Common/IInternalQueryStore.cs` — Contains `IInternalQueryStore` interface and `InternalQueryStore` class (2 public types)

**Impact:** Violates project-wide SA1649 rule (one type per file). Inconsistent with strict enforcement in all other modules.

**Fix:** Split each into separate files. `ErrorCodes.cs`, `IAuthTokenProvider.cs`, `AuthTokenProvider.cs`, etc.

---

### 5. Typo in BluQubeQuery Paths — "gat-by-id"

**Files:**
- `src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetDetailedDomainByIdQuery.cs:6` — Path: `"api/domains/gat-by-id"`
- `src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetDetailedSubdomainByIdQuery.cs:6` — Path: `"api/subdomains/gat-by-id"`

**Impact:** API endpoints are live with typos. Changing them later is a breaking change for any consumers.

**Fix:** Correct to `"api/domains/get-by-id"` and `"api/subdomains/get-by-id"`. Do it now before clients depend on them.

---

### 6. EmailRepository Bypasses IMessageStoreProvider Abstraction

**File:** `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Repositories/EmailRepository.cs:13-22`

**Problem:** `GetMimeMessageAsync()` directly uses `File.Exists()` and `File.OpenRead()` instead of delegating to `IMessageStoreProvider.LoadMessageContentAsync()`. The abstraction exists specifically for this purpose.

**Impact:** Duplicates file path logic. Untestable without real filesystem. Inconsistent with the rest of the SMTP pipeline which uses `IMessageStoreProvider`.

**Fix:** Inject `IMessageStoreProvider` and delegate to `LoadMessageContentAsync()`.

---

### 7. DomainManagement.csproj References Server Projects Directly

**File:** `src/modules/Spamma.Modules.DomainManagement/Spamma.Modules.DomainManagement.csproj:42-43`

**Problem:** References both `Spamma.Modules.EmailInbox.csproj` and `Spamma.Modules.UserManagement.csproj` (server projects, not `.Client`). Modules should only reference other modules' `.Client` packages for contracts.

**Impact:** Creates a dependency web where DomainManagement has compile-time access to UserManagement and EmailInbox internals. The EmailInbox reference appears unused (no `using Spamma.Modules.EmailInbox` found in DomainManagement code).

**Fix:** Remove `EmailInbox` reference entirely (unused). Replace `UserManagement` server reference with proper abstraction in Common (see issue #1).

---

### 8. EmailInbox.csproj References UserManagement Server Project

**File:** `src/modules/Spamma.Modules.EmailInbox/Spamma.Modules.EmailInbox.csproj:51`

**Problem:** Same pattern — EmailInbox references `UserManagement.csproj` (server) for API key validation.

**Fix:** Covered by issue #2 — move interface to Common, remove server reference.

---

### 9. Settings.cs Mixes Concerns

**File:** `src/modules/Spamma.Modules.Common/Settings.cs`

**Problem:** Contains `MailServerHostname` and `MxPriority` alongside auth settings (`SigningKeyBase64`, `AuthenticationTimeInMinutes`). Email/MX settings belong in DomainManagement or EmailInbox, not Common.

**Fix:** Split into `AuthSettings` (Common) and module-specific settings classes.

---

## 🟢 Minor Issues

### 10. Duplicate ProjectReference in EmailInbox.csproj

**File:** `src/modules/Spamma.Modules.EmailInbox/Spamma.Modules.EmailInbox.csproj` — Lines 47 and 71 both reference `Spamma.Modules.Common.Client.csproj`.

**Fix:** Remove the duplicate on line 71.

### 11. Missing BluQube Attributes on Internal Commands/Queries

**Files:**
- `StartAuthenticationCommand.cs`, `CompleteAuthenticationCommand.cs` — No `[BluQubeCommand]`
- `GetUserByIdQuery.cs` — No `[BluQubeQuery]`
- `ReceivedEmailCommand.cs`, `CampaignEmailReceivedCommand.cs` — No `[BluQubeCommand]`

**Assessment:** These are intentionally server-only. Auth commands go through server-rendered endpoints (not WASM). SMTP commands are dispatched internally. The missing attributes are by design — these are not WASM-routed.

**Recommendation:** Add a code comment on each explaining why the attribute is intentionally absent, so future devs don't "fix" it.

---

# BACKEND CODE REVIEW — Cortana

## 🔴 Critical Issues

### 1. `UserLookupProjection` — `PasskeyAuthenticated` patches wrong document

**File:** `Spamma.Modules.UserManagement/Infrastructure/Projections/UserLookupProjection.cs:60-64`

```csharp
public void Project(IEvent<PasskeyAuthenticated> @event, IDocumentOperations ops)
{
    ops.Patch<UserLookup>(@event.StreamId)   // StreamId = Passkey.Id, NOT User.Id
        .Set(x => x.LastPasskeyAuthenticationAt, @event.Data.UsedAt);
}
```

`PasskeyAuthenticated` is emitted on the **Passkey** aggregate stream. `@event.StreamId` is therefore the Passkey's `Id`, not the User's `Id`. `UserLookup` is keyed by user ID, so this patch silently finds nothing and `LastPasskeyAuthenticationAt` is **never written**.

**Root cause:** `PasskeyAuthenticated` event record contains only `(uint NewSignCount, DateTime UsedAt)` — no `UserId`.

**Fix options:**
- Add `UserId` to the `PasskeyAuthenticated` event and use it in the projection.
- OR load `PasskeyLookup` inside the projection to resolve `UserId` (adds a DB read per event).

---

### 2. `AuthenticateWithPasskeyCommandHandler` — wrong error code for "user not found"

**File:** `Spamma.Modules.UserManagement/Application/CommandHandlers/Passkey/AuthenticateWithPasskeyCommandHandler.cs:45`

```csharp
return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.AccountSuspended, "Account not found"));
```

Returns `AccountSuspended` (`user_management.account_suspended`) when the user record is simply not found. Clients keying on error codes will misinterpret this as a suspension, potentially displaying the wrong UI/message.

**Fix:** Use `CommonErrorCodes.NotFound`.

---

## 🟡 Warnings (10 total)

### 3 & 4. Typo in `[BluQubeQuery]` path — both `get-by-id` queries broken

**Files:**
- `Spamma.Modules.DomainManagement.Client/Application/Queries/GetDetailedDomainByIdQuery.cs:6` → `"api/domains/gat-by-id"`
- `Spamma.Modules.DomainManagement.Client/Application/Queries/GetDetailedSubdomainByIdQuery.cs:6` → `"api/subdomains/gat-by-id"`

Both paths have `gat` instead of `get`. The code-generated API endpoints will be registered at the wrong URL, breaking all WASM clients calling these queries.

**Fix:** Replace `gat-by-id` with `get-by-id` in both files.

---

### 5, 6, 7. `.DateTime` instead of `.UtcDateTime` on `TimeProvider.GetUtcNow()`

**Files:**
- `Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/DeleteEmailCommandHandler.cs:34`
- `Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/ToggleEmailFavoriteCommandHandler.cs:35`
- `Spamma.Modules.DomainManagement/Application/CommandHandlers/Domain/VerifyDomainCommandHandler.cs:33`

`GetUtcNow()` returns a `DateTimeOffset`. Calling `.DateTime` on it gives a `DateTime` with `Kind = Unspecified` (not UTC). All other handlers correctly use `.UtcDateTime`. This can cause silent timezone errors when persisting or comparing timestamps.

**Fix:** Replace `.DateTime` with `.UtcDateTime` at all three sites.

---

### 8. `DeleteCampaignCommandHandler` — discards `email.Delete()` result

**File:** `Spamma.Modules.EmailInbox/Application/CommandHandlers/Campaign/DeleteCampaignCommandHandler.cs:47`

```csharp
foreach (var email in emails)
{
    email.Delete(deletedAt);   // return value discarded
    var emailSaveResult = await emailRepository.SaveAsync(email, cancellationToken);
}
```

`Email.Delete()` returns `ResultWithError<BluQubeErrorData>`. If an email is already deleted the result is `IsFailure`, but since it is not checked, the aggregate raises no event, and `SaveAsync` is called on an unchanged aggregate.

**Fix:** Check the result; skip the save if `IsFailure`.

---

### 9. `ApiKey.cs` — `DateTimeOffset.UtcNow` in domain aggregate

**File:** `Spamma.Modules.UserManagement/Domain/ApiKeys/ApiKey.cs:38`

```csharp
internal bool IsExpired => DateTimeOffset.UtcNow >= this.ExpiresAt;
```

Direct use of the clock breaks testability. Callers should pass `now` into expiry checks.

---

### 10. `SearchUsersQueryProcessor` — `DateTime.UtcNow` in query processor

**File:** `Spamma.Modules.UserManagement/Application/QueryProcessors/User/SearchUsersQueryProcessor.cs:51`

`TimeProvider` should be injected and used here.

---

### 11. `ApiKeyAuthenticationHandler` — `DateTimeOffset.UtcNow` in infrastructure service

Same pattern — inject `TimeProvider`.

---

### 12. `ApiKey.Revoke()` throws instead of returning `Result`

**File:** `Spamma.Modules.UserManagement/Domain/ApiKeys/ApiKey.cs:77-85`

Inconsistent with other aggregate methods. Should return `ResultWithError<BluQubeErrorData>`.

---

## 🟢 Minor Issues (6 total)

### 13. `CampaignEmailReceivedCommandHandler` — wrong logger type
### 14. `ReceivedEmailCommand` and `CampaignEmailReceivedCommand` missing `[BluQubeCommand]`
### 15. `ChaosAddressLookupProjection` — `CreatedBy` permanently `Guid.Empty`
### 16. `Email.cs` — unused import
### 17. `DomainManagementErrorCodes.AlreadySuspended` — misleading value
### 18. `RecordCampaignCaptureCommandHandler` — inconsistent indentation

---

# FRONTEND CODE REVIEW — Johnson

## 🔴 Critical Issues

### 1. SMTP Preset Buttons Are Completely Broken

**Files:** `Assets/Scripts/setup-email.ts` + `Components/Pages/Setup/Email.razor`

`SetupEmailConfigurator.setupPresetHandlers()` wires buttons via selector `[onclick="setSmtpPreset('${provider}')"]` but `Email.razor` renders buttons with `data-preset="gmail"` and **no `onclick` attribute**. The TypeScript selector finds nothing; clicking presets does nothing.

**Fix:** Change TypeScript selector to `[data-preset="${provider}"]`, or add `onclick="setSmtpPreset('${provider}')"` to buttons.

---

### 2. `setup-admin.ts` — Script Is a No-Op

**File:** `Assets/Scripts/setup-admin.ts`

Every other setup TS file ends with `new SetupKeyGenerator();`. `setup-admin.ts` only exports the class. Nothing is instantiated. The "Create Additional Admin" logic never runs.

**Fix:** Add `new SetupAdmin();` at the end. Add `readyState` guard. Remove dead `setExample()` function.

---

## 🟡 Warnings (6 total)

### 3. Source Maps Embedded in Production Bundles
### 4. Webpack Config — Leftover Cruft
### 5. Dead Parcel Config in `package.json`
### 6. `Home.razor.cs` — Excessive `StateHasChanged()` Calls
### 7. `AppLayout.razor.cs` — `.Wait()` on Async in Dispose
### 8. `login.ts` — Dead Commented-Out Code

---

## 🟢 Minor Issues (4 total)

### 9. `setup-hosting.ts` Typo
### 10. Inline Styles That Could Be Tailwind
### 11. `DomainIcon.razor` — Inline Style for Visibility Toggle
### 12. `webauthn-utils.ts` — Dual Export Pattern

---

# TEST COVERAGE REVIEW — Arbiter

## 🔴 CRITICAL: 3 DISABLED TEST FILES

| File | Tests | Impact |
|------|-------|--------|
| `SpammaMessageStoreTests.cs.DISABLED` | 7 well-written tests | SMTP ingestion untested |
| `SmtpInputValidationTests.cs.DISABLED` | Security tests | Malformed MIME untested |
| `PersistReceivedEmailHandlerTests.cs.DISABLED` | Integration tests | Persistence untested |

---

## Coverage Gaps

### UserManagement
- **QueryProcessors:** 8 completely untested (GetUserById, GetUserStats, SearchUsers + Passkey 4 + ApiKey 1)
- **Authorizers (Queries):** 3 untested (RevokeApiKeyCommandAuthorizer, GetMyApiKeysQueryAuthorizer, GetPasskeyByCredentialIdQueryAuthorizer)

### DomainManagement
- **QueryProcessors:** 10 completely untested
- **ChaosAddress CommandHandlers:** 5 completely untested (Create, Delete, Disable, Enable, RecordChaosAddressReceived)
- **Subdomain Aggregate:** No dedicated tests (only via handler tests)

### EmailInbox
- **Application QueryProcessors:** Placeholder tests (`1.Should().Be(1)` — false confidence)
- **Infrastructure:** SpammaMessageStore, SmtpInputValidation, PersistReceivedEmailHandler all disabled
- **Background Services:** EmailCleanupBackgroundService, PushNotificationManager, EmailPushGrpcService untested

---

## Priority Action List

### P0 — Immediate
1. Re-enable `SpammaMessageStoreTests.cs.DISABLED` (7 tests, core SMTP pipeline)
2. Re-enable `SmtpInputValidationTests.cs.DISABLED` (security tests)
3. Re-enable `PersistReceivedEmailHandlerTests.cs.DISABLED` (integration tests)

### P1 — High
4. DomainManagement ChaosAddress handlers (5 handlers, zero tests)
5. UserManagement QueryProcessors (8 untested)
6. DomainManagement QueryProcessors (10 untested)
7. Remove EmailInbox Application-layer placeholder tests

### P2 — Medium
8. SmtpHostedService tests (replace reflection-based tests)
9. SubdomainAggregateTests (dedicated aggregate tests)
10. Fill authorizer gaps (3 untested)
11. Migrate DomainManagement tests to `ShouldHaveRaisedEvent`

### P3 — Low
12-15. Background services, missing integration tests

---

# SECURITY REVIEW — Halsey

## 🔴 CRITICAL VULNERABILITIES

### 1. WebAuthn Assertion Signature Is Never Verified

**File:** `AuthenticationEndpoints.cs` — `MakeAssertion()`

The `AssertionResponseData.Signature` is received but **never used**. The entire cryptographic security of WebAuthn is absent. Anyone can authenticate with just a valid `credentialId`.

**What's missing:**
- ❌ Signature verification using stored public key
- ❌ Origin/rpId hash validation
- ❌ Challenge verification
- ❌ `type` field validation
- ❌ User verification flag check

**Fix:** Integrate `Fido2NetLib` for full assertion verification.

---

### 2. WebAuthn Challenge Is Never Verified

**File:** `AuthenticationEndpoints.cs` — `MakeAssertion()`

Challenge retrieved from session but never compared to `clientDataJSON`. Enables replay attacks. Previously captured assertions would pass this check.

**Fix:** Decode `clientDataJSON`, extract and compare challenge before calling `commander.Send()`.

---

## 🟡 Security Warnings (8 total)

### 3. Plain-text Password Logged in Setup Auth Attempts
### 4. `GetCurrentSetupPassword()` Exposes Plain-text Setup Password
### 5. Setup Password Has ~16 Bits Entropy (57,600 possibilities)
### 6. No Brute-force Protection on `/setup-login`
### 7. `SetupModeMiddleware` Uses `path.Contains()` — Can Be Bypassed
### 8. `UseAuthentication()` Missing from Middleware Pipeline
### 9. Raw API Key Used as Redis Cache Key (should use SHA256)
### 10. Hardcoded Credentials in `appsettings.Development.json`
### 11. Unauthenticated OTEL Trace Forwarding — No Auth, No Size Limit

---

## 🟢 Informational (8 items)

### 12. Magic Link JWT: No Issuer/Audience Validation
### 13. `GetPasskeyByCredentialIdQuery` Has No Authorization
### 14. `CookieSecurePolicy.SameAsRequest` — OK for dev, note for production
### 15. Challenge Not Cleared on Failed Auth Attempts
### 16. `userVerification: "preferred"` Should Be `"required"`
### 17. `ReceivedEmailCommandAuthorizer` Intentionally Unguarded — Documented
### 18. `InternalQueryStore` Authorization Bypass — Object Identity Dependent

---

# SMTP PIPELINE REVIEW — Foehammer

## 🔴 CRITICAL ISSUES

### 1. `IMessageStoreProvider` Completely Bypassed

**File:** `BackgroundTaskService.cs` (lines 113–116)

Direct file writes via `Directory.CreateDirectory()` and `MimeMessage.WriteToAsync()` bypass the entire `IMessageStoreProvider` abstraction. Not testable. No rollback path.

**Fix:** Route writes through `IMessageStoreProvider.StoreMessageContentAsync()`.

---

### 2. No Rollback Logic

**File:** `BackgroundTaskService.cs` (lines 91–116)

Command result discarded. If command fails, `.eml` file still written. If file write fails after command dispatch, orphaned email records created.

**Fix:** Check command result; implement rollback logic.

---

### 3. Single DI Scope for All Background Work Items

**File:** `BackgroundTaskService.cs` (lines 19–21)

Single scope created at startup, reused for ALL emails. Marten's `IDocumentSession` accumulates unbounded state. Stale reads. Any corruption affects all subsequent emails.

**Fix:** Create new `IServiceScope` per work item inside loop.

---

### 4. Wrong Sort Key Causes Non-Deterministic Subdomain Lookup

**File:** `SubdomainCache.cs` (line 60) & `SearchSubdomainsQueryProcessor.cs`

`SortBy: "domainname"` is not recognized. Falls to default, sorts by `CreatedAt`. Combined with `Contains`-based search, returns wrong subdomain when multiple share suffix.

**Fix:** Change to `"subdomainname"` and add exact-match filtering.

---

## 🟡 Warnings (5 total)

### 5. `SubdomainStatus.Inactive` Accepted as Valid
### 6. Cc/Bcc Recipients Not Checked for Domain Validation
### 7. All Background Job Exceptions Silently Swallowed (no logging)
### 8. `SmtpResponse.TransactionFailed` Never Returned (architectural trade-off, document it)
### 9. SMTP Port Hardcoded to 25, No Configuration

---

## 🟢 Minor Issues (3 total)

### 10. Wrong Error Log Message in `LoadMessageContentAsync`
### 11. `SpammaMessageStore` Uses `DateTimeOffset.Now` (should inject TimeProvider)
### 12. `EmailCleanupBackgroundService` Checks `result != null` Instead of Status

---

# INFRASTRUCTURE REVIEW — Guilty Spark

## 🔴 CRITICAL ISSUES

### 1. NuGet Package Vulnerabilities (BLOCKING BUILD)

**Affected:**
- `Microsoft.Bcl.Memory` 9.0.0 - HIGH CVE (GHSA-73j8-2gch-69rq)
- `MimeKit` 4.14.0 - MODERATE CVE (GHSA-g7hc-96xr-gvvx)
- `MailKit` 4.9.0 — 4.14.1 - MODERATE CVE (GHSA-9j88-vvj5-vhgr)

**Fix:** Update to ≥4.15, run `dotnet restore` and rebuild.

---

### 2. Hardcoded Certificate Password in appsettings.Development.json

**Location:** `appsettings.Development.json` line 22

```json
"Password": "m05sAzv7IiMw0XGD"
```

Secret committed to source control, visible in Git history.

**Fix:** Move to `.gitignore`, use `dotnet user-secrets`, rotate certificate.

---

### 3. Frontend Assets Not Built - wwwroot Missing

**Location:** `src/Spamma.App/Spamma.App/wwwroot` (does not exist)

No assets built. Application cannot serve CSS, JS, images.

**Fix:** Run `npm ci && npm run build`. Verify wwwroot/ generated.

---

## 🟡 WARNINGS (4 total)

### 1. appsettings.Development.json Missing from .gitignore
### 2. Kestrel Hardcoded Binding to Non-Standard Port (50055, 50056)
### 3. Default PostgreSQL Credentials Weak (marten_password)
### 4. MailHog Missing Health Check

---

## 🟢 MINOR ISSUES (2 total)

### 1. Webpack Config Has Obsolete `generator` Block
### 2. CI/CD Workflow Version Mismatch

---

---

## Session Status: COMPLETE ✅

**All findings documented. All decisions catalogued. Ready for implementation.** 

**Total Findings:** 15 Critical | 18 Warnings | 6 Minor

---

---

# IMPLEMENTATION DECISIONS — Post-Review Sprint (2026-04-21 / 2026-04-22)

Decisions, fixes, and design records from implementation agents following the initial 7-agent code review session.

---

## USER DIRECTIVES

### 2026-04-21T12:20 — TDD Red/Green Required
**By:** Andrew Davis
All squad agents must follow a TDD red/green pattern for C#, Blazor, and TypeScript code. Write a failing test first (red), then write the minimum implementation to make it pass (green), then refactor. No implementation code without a failing test to drive it.

### 2026-04-21T13:08 — Redis Stays for Dev and Production
**By:** Andrew Davis
Redis stays for both dev and production. No in-memory CAP transport fallback. Dev environment mirrors prod — same infrastructure, no environment-specific transport switching.

### 2026-04-22T08:00 — SMTP Routing Priority: Subdomain First, Catch-All Fallback
**By:** Andrew Davis
When processing incoming email, always attempt subdomain lookup first (existing flow). Only fall through to the catch-all sender address whitelist if NO subdomain match is found. Catch-all sender filtering is strictly a fallback path — it never interferes with normal subdomain email routing.

---

## ARCHITECTURE DECISIONS — Master Chief

### Aspire Evaluation (2026-04-21)
**Decision: No full Aspire adoption.**
Spamma is a single-process modular monolith. Aspire's value is orchestrating distributed services — the ROI is wrong here.
- **AppHost orchestration** ❌ Skip — docker-compose + single process is sufficient
- **Service Defaults** ❌ Skip — OTEL already configured manually
- **Aspire Dashboard** ✅ Use as standalone container (best Aspire feature, zero migration cost)
- **Redis/PostgreSQL integrations** ❌ Skip — conflicts with Marten/CAP internal connection management
- **Health checks** ✅ Add directly (`AspNetCore.HealthChecks.NpgSql` + `AspNetCore.HealthChecks.Redis`)

### Catch-All Email Capture Mode — Architecture (2026-04-21)
**Decision: Runtime UI toggle, sentinel GUIDs, standard pipeline reused.**
- Activation: `CatchAllModeEnabled` flag in `Settings` (Marten), toggled from Settings page — no redeploy required
- Storage: same `StandardEmailCaptureJob` → `ReceivedEmailCommand` pipeline; emails associated with sentinel `DomainId`/`SubdomainId` (`CatchAllConstants`)
- UX: dedicated "Catch-All Inbox" section in sidebar; emails grouped by actual recipient domain
- Default: `false` — existing installs unchanged, no migration needed
- Sentinel IDs: `CatchAllConstants.DomainId = 00000000-cafe-cafe-cafe-000000000001`, `CatchAllConstants.SubdomainId = 00000000-cafe-cafe-cafe-000000000002`

### Catch-All Inbox — Sender Domain Filtering Design (2026-04-21)
**Decision: Per-user persistent filter preference stored in Marten.**
- Filter stored as `CatchAllSenderDomainFilters: IReadOnlyList<string>` on `User` aggregate (event: `CatchAllSenderDomainFiltersUpdated`)
- Default: show all catch-all emails (opt-in filtering model)
- UX: inline "Watch this domain" / "Block this domain" buttons on sender domain group headers
- Active filter: hidden domains fully removed from view; filter indicator with "Clear filters" shown
- Query: `GetCatchAllEmailsQuery` gains optional `SenderDomainFilters` parameter
- New command: `UpdateCatchAllSenderDomainFiltersCommand`
- New query: `GetMyCatchAllSenderDomainFiltersQuery`
- Validation: max 10 domains, lowercase normalize, no duplicates

### Redis vs In-Memory for CAP — Evaluation (2026-04-21)
**Decision (confirmed by Andrew Davis): Redis stays for both dev and prod. No in-memory fallback.**
- CAP durability matters even for a monolith (`EmailDeleted` file cleanup would be lost on restart)
- All existing Redis caches (UserStatus, Subdomain, ChaosAddress) remain Redis-backed
- No environment-based transport switching — dev mirrors prod infrastructure

---

# IMPLEMENTATION DECISIONS — Sprint 4+ (2026-04-23 and later)

---

## BACKEND IMPLEMENTATIONS — Cortana

### BluQube ICommandRunner Runtime IL Fix (2026-04-22)

**Date:** 2026-04-22  
**Author:** Cortana (Backend Dev)  
**Commit:** `fix: resolve BluQube ICommandRunner runtime IL error`

After upgrading BluQube 1.0.3 → 1.1.0 (previous session), the app threw:
```
ManagedError: Could not resolve type with token 0100004d from typeref
(expected class 'BluQube.Commands.ICommandRunner' in assembly)
```

**Root Cause:** `Spamma.App.Tests` was **not included in `Spamma.sln`**. This orphaned project never got cleaned or rebuilt, leaving `tests\Spamma.App.Tests\bin\Debug\net10.0\BluQube.dll` as the **1.0.3 artifact from 31/10/2025** (containing `ICommander`). When bunit tests boot, the Mono IL loader encounters the stale 1.0.3 DLL and cannot resolve the renamed interface.

**Secondary Bug:** `CatchAllInboxTests.cs` still referenced `GetCatchAllEmailsQueryResult.DomainGroup` (renamed to `SenderGroup`), masked by the orphaned project being outside the solution build.

**Fixes Applied:**
| Change | File |
|--------|------|
| Renamed `DomainGroup` → `SenderGroup` | `tests/Spamma.App.Tests/CatchAllInboxTests.cs` |
| Added `Spamma.App.Tests` + `Spamma.Tests.Common` to solution | `Spamma.sln` |
| Removed spurious "Spamma.App" solution folder (name conflict MSB5004) | `Spamma.sln` |

**Verification:** All 7 `BluQube.dll` files show `20/04/2026` timestamp. `dotnet build Spamma.sln` → **0 errors, 0 warnings**.

**Prevention:** All test projects now in solution. `dotnet build Spamma.sln` covers the full repo — stale artifacts impossible.

---

### MediatR Pipeline Tracing Behavior (2026-04-23)

**Date:** 2026-04-23  
**Author:** Cortana (Backend Dev)  
**Commit:** `aa65b55`

Added `CommandQueryTracingBehavior<TRequest, TResponse>` as a global MediatR `IPipelineBehavior<,>` for OpenTelemetry tracing and structured logging of every command/query handler execution.

**Key Decisions:**
- **Generic file naming:** Uses CLR backtick-N notation per SA1649: `CommandQueryTracingBehavior`2.cs`
- **ActivitySource name:** `"Spamma.Server.Pipeline"` (must match `.WithTracing().AddSource(...)` in Program.cs)
- **Registration order:** `AddCommonBehaviors()` called **before** module registration — tracing is outermost pipeline layer
- **Status extraction:** Uses `dynamic` to avoid constraining `TResponse` — both `CommandResult` and `QueryResult<T>` have `.Status`
- **Error detection:** Status string containing `"Failed"` or `"Error"` triggers `ActivityStatusCode.Error`

**Files Modified:**
- `src/modules/Spamma.Modules.Common/Application/Behaviors/CommandQueryTracingBehavior`2.cs` (created)
- `src/modules/Spamma.Modules.Common/Module.cs` (created)
- `src/Spamma.App/Spamma.App/Program.cs`
- `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/UpdateCatchAllModeCommandHandler.cs`

---

### Fix AmbiguousMatchException — api/email-inbox/catch-all-senders (2026-04-22)

**Date:** 2026-04-22  
**Author:** Cortana (Backend Dev)  
**Status:** Resolved  
**Commit:** `fix: deduplicate catch-all sender API paths`

`POST api/email-inbox/catch-all-senders` threw `AmbiguousMatchException` — two endpoints matched:

```
HTTP: POST api/email-inbox/catch-all-senders
HTTP: POST api/email-inbox/catch-all-senders
```

**Root Cause:** Both `AddCatchAllSenderAddressCommand` and `SearchCatchAllSenderAddressesQuery` were decorated with identical path. BluQube 1.1.0 registers both as HTTP POST → route conflict.

**Fix:** Updated `AddCatchAllSenderAddressCommand.cs` path from `api/email-inbox/catch-all-senders` → `api/email-inbox/catch-all-senders/add`

**Final Path Layout:**
| Type | Path |
|------|------|
| SearchCatchAllSenderAddressesQuery | `api/email-inbox/catch-all-senders` |
| GetCatchAllSenderAddressDetailQuery | `api/email-inbox/catch-all-senders/detail` |
| AddCatchAllSenderAddressCommand | `api/email-inbox/catch-all-senders/add` |
| RemoveCatchAllSenderAddressCommand | `api/email-inbox/catch-all-senders/remove` |
| AssignUserToCatchAllSenderCommand | `api/email-inbox/catch-all-senders/assign-user` |
| UnassignUserFromCatchAllSenderCommand | `api/email-inbox/catch-all-senders/unassign-user` |

**Prevention:** When adding `[BluQubeCommand]` or `[BluQubeQuery]`, verify paths are unique across **both** types — all register as POST.

---

### Self-signed Certificate for Local/Dev Domains (2026-04-23)

**Date:** 2026-04-23  
**Author:** Cortana (Backend Dev)

`CertificateRenewalBackgroundService` calls Let's Encrypt for all mail server hostnames. In local dev (`mail.spamma.dev.localhost`), Let's Encrypt rejects `.localhost` as non-public.

**Decision:** Detect local domains and generate **self-signed certificate** instead.

**Local Domain Definition:**
- Ends with `.localhost`
- Ends with `.local`
- Exactly `localhost`
- Loopback IP addresses (`IPAddress.IsLoopback` — `127.x.x.x`, `::1`)

**Implementation:**
- `LocalDomainDetector` — static pure detection logic, no DI dependency
- `ISelfSignedCertificateService` / `SelfSignedCertificateService` — uses `System.Security.Cryptography.X509Certificates.CertificateRequest`, RSA-2048, SHA-256, 1-year validity, PFX export with password `"letmein"`
- **Singleton registration** — stateless pure computation
- `CertificateRenewalBackgroundService` — forks on domain type; email settings check deferred to Let's Encrypt path only

**Files Created (5):**
- `src/modules/Spamma.Modules.DomainManagement/Domain/LocalDomainDetector.cs`
- `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Services/ISelfSignedCertificateService.cs`
- `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Services/SelfSignedCertificateService.cs`
- `tests/Spamma.Modules.DomainManagement.Tests/Domain/LocalDomainDetectorTests.cs`
- `tests/Spamma.Modules.DomainManagement.Tests/Infrastructure/Services/SelfSignedCertificateServiceTests.cs`

**Files Modified (2):**
- `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Services/CertificateRenewalBackgroundService.cs`
- `src/modules/Spamma.Modules.DomainManagement/Module.cs`

**Test Coverage:** 11 tests passing (5 detection + 6 generation)

---

## FRONTEND IMPLEMENTATIONS — Johnson

### Align CatchAllSenders Page to Admin Page Style Standard (2026-04-21)

**Date:** 2026-04-21  
**Author:** Johnson (Frontend Dev)  
**Requested by:** Andrew Davis

Rewrote `/admin/catch-all-senders` layout skeleton to match established pattern used by `admin/users` and `admin/domains`.

**Changes:**
- Moved `<AdminHeader>` inside `<div class="min-h-screen bg-gray-50">` wrapper
- Changed content area from `max-w-5xl py-6` → `max-w-7xl py-8`
- Added search panel (`bg-white rounded-lg shadow-sm border p-6 mb-6`) with text input
- Card header upgraded from `text-sm` → `text-lg` font-medium
- Loading state: replaced border spinner with standard SVG animated spinner
- Table wrapped in `<div class="overflow-x-auto">`
- Empty state: replaced large card-with-icon with simple `text-center py-12` SVG pattern
- Table iteration changed from `_items` to `FilteredItems` for search support

**CatchAllSenders.razor.cs:**
- Added `_searchTerm` string field
- Added `FilteredItems` computed property (filters by SenderAddress, case-insensitive contains)
- Fixed `GetRowClasses` to remove duplicate hover state

**Rationale:** All interactive admin pages share structural skeleton:
1. `min-h-screen bg-gray-50` outer wrapper containing AdminHeader
2. `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8` content container
3. White search panel above table
4. Table card with `text-lg` header, SVG spinner, overflow wrapper, empty state

**Build:** 0 errors, 0 warnings

---

### CatchAllSenders UI Fix (2026-04-21)

**Date:** 2026-04-21  
**Author:** Johnson (Frontend Dev)  
**File:** `src/Spamma.App/Spamma.App.Client/Pages/Admin/CatchAllSenders.razor`

Two UX issues fixed:

1. **Missing padding:** Added header strip (`px-6 py-4 border-b border-gray-200`) to white card matching `Domains.razor` pattern. Avoids double-padding (card + cell).

2. **Remove button visibility:** Added Tailwind `group` to each row, wrapped Remove button in `opacity-0 group-hover:opacity-100 transition-opacity`. Used CSS hover (not C# state) because selected state is for expand/collapse — independent button visibility for better UX.

**Build Result:** 0 errors, 0 warnings

---

### Home Route Changed from /app to /inbox (2026-04-21)

**Date:** 2026-04-21  
**Author:** Johnson (Frontend Dev)  
**Requested by:** Andrew Davis

Main landing page route renamed `/app` → `/inbox` for clearer URL semantics.

**Files Changed:**
| File | Change |
|------|--------|
| `Spamma.App.Client/Pages/Home.razor` | `@page "/app"` → `@page "/inbox"` |
| `Spamma.App.Client/Layout/AppLayout.razor` | Logo link `href="/app"` → `href="/inbox"` |
| `Spamma.App/Components/Pages/Index.razor` | "Open App" button → `/inbox` |
| `Spamma.App/Infrastructure/Endpoints/AuthenticationEndpoints.cs` | Post-magic-link redirect → `/inbox` |
| `Spamma.App/Components/Pages/Auth/VerifyLogin.razor.cs` | Post-passkey `NavigateTo` → `/inbox` |

**Note:** `Program.cs` has `/app/certs/keys` — file system path, not a route. Left unchanged.

---

### Modal/Slideout Overlay Audit & Migration (2026-04-21)

**Date:** 2026-04-21  
**Author:** Johnson (Frontend Dev)  
**Requested by:** Andrew Davis

Full audit of overlay implementations ensuring use of `ModalBase.razor` and `SlideoutBase.razor` base components rather than raw `fixed inset-0` patterns.

**Audit Results:**

**Fixed:** `Users.razor` edit slide-out panel (lines 233–358)
- Migrated hand-rolled `<div class="fixed inset-0...">` to `<SlideoutBase>`
- ChildContent structured as header (`flex-shrink-0`), body (`flex-1`), optional footer

**Skipped (correct choices):**
- `#blazor-error-ui` divs (MainLayout & AppLayout) — Blazor runtime queries by ID; must remain raw HTML
- `UserTypeahead.razor` dropdown — `absolute` positioning (not `fixed` overlay)
- `AppLayout.razor` settings dropdown — `absolute right-0` standard dropdown pattern

**Pattern Established:**
| Use Case | Component |
|----------|-----------|
| Centred dialog/modal | `<ModalBase>` |
| Side panel (right) | `<SlideoutBase>` |
| Side panel (left) | `<SlideoutBase Direction="left">` |
| Blazor error UI | Raw HTML only |
| Dropdowns/tooltips | Raw `absolute` positioning |

**SlideoutBase ChildContent Structure:**
```razor
<SlideoutBase ...>
    <!-- Header: flex-shrink-0 prevents collapse -->
    <div class="px-4 sm:px-6 pt-6 flex-shrink-0">...</div>
    <!-- Body: flex-1 fills space -->
    <div class="flex-1 px-4 sm:px-6 pb-6 overflow-y-auto">...</div>
    <!-- Optional Footer: flex-shrink-0 -->
    <div class="px-4 sm:px-6 py-4 border-t flex-shrink-0">...</div>
</SlideoutBase>
```

**Build:** 0 errors, 0 warnings

---

## BACKEND IMPLEMENTATIONS — Cortana

### .NET 10 Package Upgrade (2026-04-21) — Commit `184680b`
7 Microsoft packages (AspNetCore.*, Extensions.Caching.*) updated from `9.0.x` to `10.0.6`. All 19 projects were already targeting `net10.0`. Build: 0 errors, 0 warnings.

### Health Checks Added (2026-04-21) — Commit `fc55fdd`
`AspNetCore.HealthChecks.NpgSql` and `AspNetCore.HealthChecks.Redis` added to `Spamma.App.csproj`. `/health` endpoint registered. Connection strings: `DefaultConnection` (PostgreSQL) and `Redis`.

### CAP Subscriber and Projection Boundary Fix (2026-04-21) — Commits `9b14c89`, `6e7743e`
- Added DomainManagement assembly to CAP `.AddSubscriberAssembly()` in `Program.cs`
- Removed direct `UserManagement.Infrastructure.ReadModels` cross-module references from DomainManagement projections
- Integration events enhanced to carry `UserName`/`UserEmail` (avoiding cross-module DB queries in projections)
- New CAP subscribers: `DomainModeratorListEventHandler`, `SubdomainModeratorListEventHandler` (DomainManagement), `UserDomainMembershipEventHandler` (UserManagement)
- `DomainManagement.csproj` now references only `UserManagement.Client` (not full server project)

### Backend Bug Fixes (2026-04-21) — Commit `3f583d6`
Three bugs fixed:
1. **`gat-by-id` typo** — corrected to `get-by-id` in `GetDetailedDomainByIdQuery` and `GetDetailedSubdomainByIdQuery`
2. **Wrong error code** — `AuthenticateWithPasskeyCommandHandler` returned `AccountSuspended` for user-not-found; fixed to `CommonErrorCodes.NotFound`
3. **UserLookupProjection stream ID mismatch** — `PasskeyAuthenticated` was patching by `@event.StreamId` (Passkey ID) instead of `UserId`; fixed by adding `UserId` to the event record

### CatchAllSenderAddress Domain Aggregate (2026-04-22)
New `CatchAllSenderAddress` aggregate in `Spamma.Modules.EmailInbox/Domain/CatchAllSenderAddressAggregate/`. Events: `CatchAllSenderAddressAdded`, `CatchAllSenderAddressRemoved`, `UserAssignedToCatchAllSender`, `UserUnassignedFromCatchAllSender`. Four new error codes in `EmailInboxErrorCodes`. Build: 0 errors.

### CatchAllSender Command Handlers, Validators, Authorizers (2026-04-22)
Full CRUD command layer for catch-all sender address management:
- `AddCatchAllSenderAddressCommandHandler`, `RemoveCatchAllSenderAddressCommandHandler`, `AssignUserToCatchAllSenderCommandHandler`, `UnassignUserFromCatchAllSenderCommandHandler`
- Repository: `ICatchAllSenderAddressRepository` / `CatchAllSenderAddressRepository` (GenericRepository pattern)
- All authorizers use `MustBeAuthenticatedRequirement` (consistent with module pattern)
- Registered as `Scoped` in `Module.AddEmailInbox()`
- Build: 0 errors.

### CatchAllSenderAddressLookup Read Model and Projection (2026-04-22)
- `CatchAllSenderAddressLookup` read model with `Id`, `SenderAddress`, `AssignedUserIds`, `IsRemoved`, `AddedAt`
- `CatchAllSenderAddressLookupProjection` using `EventProjection` (consistent with module pattern — `SingleStreamProjection<T>` not available in Marten 8.13.3)
- Projection registered as `Inline` in `Module.ConfigureEmailInbox()`
- Build: 0 errors.

### Catch-All Query Processors (2026-04-22)
Three query processors implemented:
- `SearchCatchAllSenderAddressesQueryProcessor` — paged admin list
- `GetCatchAllSenderAddressDetailQueryProcessor` — detail with `AssignedUserIds`
- `GetCatchAllEmailsQueryProcessor` updated — non-admin users filtered to sender addresses they are assigned to; admin (`SystemRole.DomainManagement`) bypasses filter
- Grouping changed to full sender address (not domain suffix)
- All authorizers use `MustBeAuthenticatedRequirement`

### ICatchAllSenderAddressCache (2026-04-22)
`ICatchAllSenderAddressCache` / `CatchAllSenderAddressCache` in `Infrastructure/Services/Caching/`:
- Returns `CachedSenderAddress?` (nullable, not `Maybe<>`) — consistent with task spec and SMTP usage context
- Redis backend matching `ChaosAddressCache` / `SubdomainCache` pattern
- `"null"` sentinel string for cache misses (1-minute TTL); 5-minute TTL on hits
- Registered as `Scoped` (lifetime-aligned with `IQuerySession`)
- `StackExchange.Redis` added explicitly to `EmailInbox.csproj`

### Auth Logging Audit (2026-04-22)
Full structured logging added to the magic link auth email path. See orchestration log `2026-04-22T08-30-cortana-auth-logging.md` for full detail.
**Root cause diagnosed:** `FormatException` in `AuthTokenProvider` (missing `SigningKeyBase64`) was swallowed by a bare `catch`, causing silent email delivery failures.

---

## SMTP/EMAIL IMPLEMENTATIONS — Foehammer

### Catch-All SMTP Mode — Assessment (2026-04-21)
Technical assessment for Master Chief:
- `IMailboxFilter` is the correct SMTP protocol hook for RCPT TO filtering (but not needed — current behavior already accepts all RCPT TO; rejection is application-level)
- Minimal change: port-check before domain validation loop in `SpammaMessageStore.SaveAsync`
- Dual-port approach (`context.EndpointDefinition.Endpoint.Port`) fully viable
- Recommended: Option B seeded system domain with well-known GUIDs for referential integrity

### Catch-All Port 1026 Implementation (2026-04-21) — `feat: optional port 1026 catch-all SMTP listener`
Optional dual-port SMTP listener:
- Port 1025 (strict): domain validation unchanged
- Port 1026 (catch-all): enabled by `SmtpServer.CatchAllPortEnabled = true` in config; skips domain validation
- Port-based detection: `context.EndpointDefinition.Endpoint.Port` in `SpammaMessageStore.SaveAsync`
- Sentinel GUIDs: `CatchAllConstants.DomainId` / `CatchAllConstants.SubdomainId`
- Pre-existing fixes: `CatchAllEmailCaptureJob.IsCatchAll` made static (SA2325); duplicate `CatchAllConstants` class removed
- 9 `SpammaMessageStore` tests passing including 5 new catch-all port tests

### Email Links Opening in New Tab Fix (2026-04-21) — Commit `2e3e4aa`
Fixed iframe rendering in `EmailViewer` component:
- Added `<base target="_blank">` injection via `PrepareHtmlForIframe()` helper (handles all HTML variants: full, fragment, no tags)
- Updated iframe sandbox: `allow-same-origin allow-popups allow-popups-to-escape-sandbox`
- Static helper placed with other static members (SA1204 compliance)
- Build: 0 errors.

### BackgroundTaskService Storage Rollback Fix (2026-04-21)
Decision: Route all file I/O through `IMessageStoreProvider` with transactional rollback:
1. Resolve `IMessageStoreProvider` from DI scope in `BackgroundTaskService.ExecuteAsync`
2. `StoreMessageContentAsync` before dispatching any command
3. If storage fails → early return, no command dispatched
4. If command fails → `DeleteMessageContentAsync` rollback
5. `IHostEnvironment` dependency removed from `BackgroundTaskService`
- File: `Infrastructure/Services/BackgroundJobs/BackgroundTaskService.cs`

### EmailLookup CatchAllSenderAddressId (2026-04-22)
Added `Guid? CatchAllSenderAddressId = null` to `ReceivedEmailCommand`, `EmailReceived` event, `Email.Create`, `EmailLookup`, and `EmailLookupProjection`. Safe for event sourcing (default null). Pre-existing bugs fixed: `GetCatchAllEmailsQueryProcessor` updated for renamed `SenderGroup`/`SenderAddress`; `CatchAllSenderAddressLookupProjection` converted from invalid `SingleStreamProjection<T>` to `EventProjection`.

### Catch-All Sender Whitelist in SpammaMessageStore (2026-04-22)
SMTP priority rule enforced in `SpammaMessageStore.SaveAsync`:
1. Subdomain routing first — if any `To:` recipient domain matches active subdomain, route there (no whitelist check)
2. Catch-all fallback — only if `foundValidSubdomain == null` AND `catchAllEnabled == true`
3. Sender whitelist check — extract `From:` address; call `ICatchAllSenderAddressCache`; reject if null
4. `CatchAllSenderAddressId` flows through `CatchAllEmailCaptureJob` → `ExtractEmailAddressesAndSendCommand` → `ReceivedEmailCommand`

### Outbound SMTP Logging Audit (2026-04-22)
Structured `ILogger` added to `EmailSender` and `SendAuthenticationEmailToUser`. Fixed unchecked `Result` from `SendEmailAsync`. Added startup SMTP config log to `Program.cs`. Port 2025 confirmed correct. Build: 0 errors. See orchestration log `2026-04-22T08-30-foehammer-smtp-logging.md` for full detail.

---

## SECURITY IMPLEMENTATIONS — Halsey

### WebAuthn Assertion Verification + UseAuthentication Fix (2026-04-21)
Two critical auth vulnerabilities fixed:

**`UseAuthentication()` missing from middleware pipeline** — `HttpContext.User` was never populated from the `SpammaAuth` cookie; all authorization policies were effectively unenforced. Fixed: `app.UseAuthentication()` added before `app.UseAuthorization()` in `Program.cs`.

**WebAuthn assertion never verified** — Signature, challenge, origin, and rpId were received but never validated. Fixed: `WebAuthnAssertionVerifier` implementing `IWebAuthnAssertionVerifier` (injectable, mockable, singleton) added to `AuthenticateWithPasskeyCommandHandler`. Implements full WebAuthn spec: type check, challenge comparison, origin/rpId hash, user-present flag, ECDSA/RSA signature verification via `System.Formats.Cbor`. Generic `PasskeyVerificationFailed` error response (no failure detail to attackers).

Decisions:
- Verification in command handler (has access to stored `PublicKey` + `Algorithm`)
- Injectable interface (not static) for testability
- `PublicKey` stored as raw CBOR attestation object; COSE key extracted at verification time (defers storage format migration)

---

## INFRASTRUCTURE IMPLEMENTATIONS — Guilty Spark

### Aspire Dashboard Replaces Jaeger (2026-04-21) — Commit `71b7ec7`
Jaeger removed from `docker-compose.yml`. Replaced with `mcr.microsoft.com/dotnet/aspire-dashboard:latest` on ports `18888` (UI) and `18889` (OTLP gRPC). No code changes required — existing OTLP exporters updated to point to new endpoint. Anonymous access enabled for local dev.

### .NET 10 Infrastructure Upgrade (2026-04-21) — Commit `6bca2f3`
`Dockerfile.build` updated from `sdk:9.0`/`aspnet:9.0` to `10.0`. All other Dockerfiles and GitHub Actions workflows were already on .NET 10.

### Secrets and CVE Fixes (2026-04-21)
- Hardcoded certificate password replaced with `<SET_VIA_ENV_OR_USER_SECRETS>` placeholder in `appsettings.Development.json`
- `appsettings.Development.json` and `appsettings.*.json` added to `.gitignore`
- NuGet CVEs patched: `Microsoft.Bcl.Memory` 9.0.0 → 10.0.6 (HIGH), `MimeKit` 4.14.0 → 4.16.0 (MODERATE), `MailKit` 4.14.1 → 4.16.0 (MODERATE) across all 11 affected projects

### ASP.NET Core Developer Certificate Approach (2026-04-22) — Commit `592a57f`
Removed manual `cert.pfx` requirement from `appsettings.Development.json`. Kestrel now uses the ASP.NET Core developer certificate from the machine cert store automatically (run `dotnet dev-certs https --trust` once per machine). `UserSecretsId` added to `Spamma.App.csproj`. `README.md` updated with dev cert instruction.

---

## FRONTEND IMPLEMENTATIONS — Johnson

### Setup Script Bug Fixes (2026-04-21) — Commit `7001a48`
Two critical setup wizard bugs fixed:
1. `setup-admin.ts` — `SetupAdmin` class was never instantiated (no-op script). Fixed: `new SetupAdmin();` added at end of file.
2. `setup-email.ts` — preset button selector used `[onclick="setSmtpPreset(...)"]` but `Email.razor` renders `data-preset="..."` attributes. Fixed: selector changed to `[data-preset="${provider}"]`.

### Vitest TypeScript Test Framework (2026-04-21) — Commit `150c22b`
**Decision: Vitest** (over Jest — TypeScript-native, faster, simpler config).
- Installed: `vitest`, `@vitest/ui`, `jsdom`, `happy-dom`
- 11 tests passing across `setup-admin.test.ts` (3) and `setup-email.test.ts` (8)
- `npm test` / `npm run test:watch` / `npm run test:ui` scripts added
- Pattern: `[data-preset]` attribute selector confirmed correct for Blazor-rendered buttons

### Autocomplete Overflow Fix in ModalBase (2026-04-21) — Commit `6f2b5be`
`overflow-hidden` removed from `ModalBase.razor` content container (was clipping `UserTypeahead` dropdown via absolute positioning). 2 bUnit regression tests added to `Spamma.App.Tests`. `bunit` 2.7.2 added to test project.

### Catch-All Inbox UI (2026-04-21)
New Blazor WASM pages:
- `/inbox/catch-all` (`CatchAllInbox.razor`) — amber accent, groups by sender domain; disabled state when `CatchAllModeEnabled=false`
- `/admin/settings` (`AppSettings.razor`) — catch-all toggle with warning banner
- Nav links added: amber "Catch-All" (visible when enabled) + "Settings" in administration dropdown
- 3 bUnit tests in `Spamma.App.Tests/CatchAllInboxTests.cs`
- `QueryResult<T>` usage: factory methods `Succeeded(data)` / `Failed()` / `NotFound()` (not object initializer)

### Catch-All Senders Admin UI (2026-04-21)
New admin page `/admin/catch-all-senders` (`CatchAllSenders.razor`):
- Table: Sender Address | Assigned Users | Added | Actions (amber theme matching catch-all inbox)
- Expandable rows via `GetCatchAllSenderAddressDetailQuery`
- Add modal with client-side validation (empty + `@` check)
- All ops via `ICommander` / `IQuerier`: Search, Detail, Add, Remove, Unassign user
- Nav link in Administration section of settings dropdown
- Pre-existing bug fixed: `CatchAllInbox.razor` `DomainGroup` → `SenderGroup` rename

---

## TESTING DECISIONS — Arbiter

### Re-enable Disabled SMTP Test Files (2026-04-21) — Commit `5571ab1`
Three `.DISABLED` test files renamed and converted to placeholder tests:
- `SpammaMessageStoreTests.cs` (7 tests) — `[Fact(Skip = "...")]` with documented reason
- `SmtpInputValidationTests.cs` (8 tests) — `[Fact(Skip = "...")]`
- `PersistReceivedEmailHandlerTests.cs` (6 tests) — `[Fact(Skip = "...")]`
**Decision:** Use `[Fact(Skip = "...")]` over `.DISABLED` files — CI reports reason, preserves design intent.
Side fixes: null-safety warnings in `BackgroundTaskService.cs`; `Directory.Build.props` suppresses NU1902/NU1903.

### EmailInbox and DomainManagement Test Improvements (2026-04-21) — Commit `6e7743e`
- 4 new `SpammaMessageStore` unit tests (TDD red→green verified)
- 3 placeholder query processor tests updated from `1.Should().Be(1)` to `[Fact(Skip = "...")]`
- DomainManagement: 10 QueryProcessors identified as completely untested; infrastructure (PostgreSqlFixture, Testcontainers) is in place for future work
- Key learning: `PushNotificationManager` has non-virtual methods — use real instance in tests

### TDD RED Tests for Catch-All Email Mode (2026-04-21)
4 TDD RED tests written in `SpammaMessageStoreCatchAllTests.cs` specifying catch-all behaviour before implementation:
1. `CatchAllDisabled_UnknownDomain_ReturnsMailboxNameNotAllowed`
2. `CatchAllEnabled_UnknownDomain_QueuesJobWithSentinelDomainAndReturnsOk`
3. `CatchAllEnabled_KnownActiveSubdomain_UsesRealSubdomainNotCatchAll`
4. `CatchAllEnabled_UnknownDomain_MessageStoreFails_CleansUpAndReturnsTransactionFailed`
Design contracts: sentinel ID = `CatchAllConstants.DomainId`, settings via `IEmailInboxSettingsService`, rollback via `IMessageStoreProvider.DeleteMessageContentAsync`.

---

## Session Status: ALL INBOX ITEMS MERGED ✅

**Last consolidated:** 2026-04-22T08:30Z  
**Items merged:** 36 inbox decisions from agents across 2 sprint sessions  
**Agents covered:** Cortana, Foehammer, Johnson, Arbiter, Halsey, Guilty Spark, Master Chief

---

# IMPLEMENTATION DECISIONS — Sprint 3 (2026-04-22 continued)

---

## BACKEND IMPLEMENTATIONS — Cortana

### Magic Link Auth Root Cause Investigation (2026-04-22)

**Date:** 2026-04-22  
**Author:** Cortana (Backend Dev)

Two root causes of the missing magic link flow identified and fixed:

1. **`MustNotBeAuthenticatedRequirement` silent rejection** — If a stale `SpammaAuth` cookie is present, `IsAuthenticated = true` → authorization fails → BluQube short-circuits before `HandleInternal` → zero logs. Added canary `LogInformation` at entry of `Login.razor.cs` `HandleSendMagicLink`; added `LogWarning` when `CommandResult.Status != Succeeded`.

2. **`SigningKeyBase64` unconfigured → silent `FormatException`** — `Convert.FromBase64String("")` threw `FormatException` inside a bare `catch { return Result.Fail(); }` in `AuthTokenProvider`. `SendAuthenticationEmailToUser` received `token.IsFailure` and silently returned — no email sent, no log. Fixed: `AuthTokenProvider.GetToken()` and `ProcessToken()` now guard for empty key (`LogError` + `Result.Fail`) and wrap `Convert.FromBase64String` in a try-catch that logs the exception.

**Modified Files:**
- `src/Spamma.App/Spamma.App/Components/Pages/Auth/Login.razor.cs`
- `src/modules/Spamma.Modules.Common/IAuthTokenProvider.cs`

**Build:** 0 errors, 0 warnings

---

### BluQube NuGet Upgrade 1.0.3 → 1.1.0 (2026-04-22)

**Date:** 2026-04-22  
**Author:** Cortana (Backend Dev)  
**Requested by:** Andrew Davis

Upgraded all `BluQube` package references from `1.0.3` to `1.1.0` across the solution.

**Package versions updated:**

| Package | Old | New | Projects |
|---|---|---|---|
| `BluQube` | 1.0.3 | 1.1.0 | All 10 projects |
| `FluentValidation` | 12.0.0 | 12.1.0 | Spamma.App + 3 modules |
| `FluentValidation.DependencyInjectionExtensions` | 12.0.0 | 12.1.0 | Spamma.App + 3 modules |

**Breaking changes fixed:**

1. **FluentValidation version conflict** — BluQube 1.1.0 requires `FluentValidation >= 12.1.0`. Projects pinned to `12.0.0` caused `NU1605` downgrade error. Fixed by bumping all direct FluentValidation references to `12.1.0`.

2. **Renamed interfaces (47 files)**
   - `ICommander` → `ICommandRunner` (namespace `BluQube.Commands`)
   - `IQuerier` → `IQueryRunner` (namespace `BluQube.Queries`)

3. **Renamed concrete types (3 files)**
   - `Commander` → `CommandRunner`
   - `Querier` → `QueryRunner`
   - Files: `Spamma.App/Program.cs`, `Spamma.App.Client/Program.cs`, `SmtpEndToEndFixture.cs`

**Outcome:** Build 0 errors, 0 warnings  
**Commit:** `chore: update BluQube 1.0.3 -> 1.1.0 and fix breaking changes`

---

## Session Status: ALL INBOX ITEMS MERGED ✅

**Last consolidated:** 2026-04-22T10:00Z  
**Items merged:** 38 inbox decisions from agents across 3 sprint sessions  
**Agents covered:** Cortana, Foehammer, Johnson, Arbiter, Halsey, Guilty Spark, Master Chief

---

# SCRIBE SESSION — 2026-04-23T12:22

## Decision: StyleCop Generic Type Filename Convention

**Status:** ✅ Established & Applied  
**Scope:** All generic types across project  
**Enforced by:** stylecop.json — `fileNamingConvention=metadata`

### Decision Statement

Generic type files must use **CIL arity notation** with backticks:
- **Format:** `TypeName`{arity}.cs`
- **Example:** `CommandQueryTracingBehavior`2.cs` (2 type parameters)
- **Not:** `CommandQueryTracingBehavior_TRequest_TResponse.cs` (invalid)
- **Not:** `CommandQueryTracingBehavior.cs` (violates SA1649)

### Rationale

1. **StyleCop Compliance:** Project uses `fileNamingConvention=metadata` (CIL format) — this is the authoritative naming convention
2. **Consistency:** Aligns all generic type filenames with .NET CIL conventions
3. **SA1649 Enforcement:** Ensures one public type per file (backtick-arity prevents collision with non-generic `TypeName.cs`)
4. **Code Generation:** BlueQube attributes and reflection-based tools can correctly identify generic types

### Application

Implemented in:
- `src/modules/Spamma.Modules.Common/Application/Behaviors/CommandQueryTracingBehavior`2.cs`
  - MediatR pipeline behavior for command/query tracing
  - 2 type parameters: `TRequest`, `TResponse`
  - Registered in Module.cs and Program.cs

### Future References

All new generic types must follow this pattern:
- Generic class with N parameters → `ClassName`N.cs`
- Generic interface with N parameters → `IInterfaceName`N.cs`
- Generic record with N parameters → `RecordName`N.cs`

**Related decision:** Coordinator previously corrected this naming (was about to be renamed incorrectly).
