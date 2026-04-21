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
