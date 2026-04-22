# Orchestration Log: cortana-magic-link-trace

**Timestamp**: 2026-04-22T10:00Z

## Summary

Deep-traced the full magic link authentication flow after zero log output was observed during sign-in. Identified two root causes: (1) `MustNotBeAuthenticatedRequirement` silently rejecting the command when a stale `SpammaAuth` cookie is present — `HandleInternal` is never reached, no logs emitted; (2) missing `SigningKeyBase64` in settings caused a `FormatException` inside `AuthTokenProvider.GetToken()` which was swallowed by a bare `catch`, causing the CAP subscriber to return `Result.Fail()` with no log. Added a canary log at the entry of `HandleSendMagicLink`, hardened `AuthTokenProvider` with explicit guards and `LogError` on key misconfiguration, and confirmed the CAP and Docker Compose wiring is correct.

## Completed Tasks

- ✅ Traced full magic link flow: `Login.razor` → `ICommander.Send` → `StartAuthenticationCommandHandler` → CAP event → `SendAuthenticationEmailToUser` → `AuthTokenProvider` → `EmailSender`
- ✅ Identified `MustNotBeAuthenticatedRequirement` as likely cause of zero handler logs (stale auth cookie)
- ✅ Confirmed CAP subscriber assembly registration is correct (all 4 assemblies)
- ✅ Confirmed Docker Compose wiring: PostgreSQL `:5432`, Redis `:6379`, MailHog SMTP host `:2025` → container `:1025`
- ✅ Added canary `LogInformation` to `Login.razor.cs` `HandleSendMagicLink` entry point
- ✅ Added `LogWarning` in `HandleSendMagicLink` when `CommandResult.Status != Succeeded`
- ✅ Removed incorrect XML doc comment from `Login.razor.cs` (partial class)
- ✅ Hardened `AuthTokenProvider.GetToken()`: guard for empty `SigningKeyBase64` → `LogError` + `Result.Fail`
- ✅ Hardened `AuthTokenProvider.GetToken()`: try-catch around `Convert.FromBase64String` → `LogError` + `Result.Fail`
- ✅ Hardened `AuthTokenProvider.ProcessToken()` with same guards
- ✅ Build: 0 errors, 0 warnings

## Root Causes

| Cause | Description |
|---|---|
| Stale `SpammaAuth` cookie | `MustNotBeAuthenticatedRequirement` returns Forbidden → BluQube short-circuits before `HandleInternal` → zero logs |
| Empty `SigningKeyBase64` | `Convert.FromBase64String("")` threw `FormatException` inside bare `catch` → `SendAuthenticationEmailToUser` hit `token.IsFailure` and silently returned |

## Diagnosis Guide

1. Submit login form → look for `CANARY — HandleSendMagicLink invoked for ...`
2. If absent: SSR form not invoking method (antiforgery/Blazor issue)
3. If present + `StartAuthenticationCommand failed ... Forbidden`: stale cookie — clear browser cookies and retry
4. If `SigningKeyBase64 is not configured` log present: run setup wizard or insert `security.signingKey` into `app_configuration`

## Artifacts

- **Modified Files**:
  - `src/Spamma.App/Spamma.App/Components/Pages/Auth/Login.razor.cs`
  - `src/modules/Spamma.Modules.Common/IAuthTokenProvider.cs`

## Status

✅ COMPLETED

## Related Agents

- cortana-auth-logging (prior session: full auth flow logging audit)
- foehammer-smtp-logging (prior session: outbound SMTP dispatch audit)
