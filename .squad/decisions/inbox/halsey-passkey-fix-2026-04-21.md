# Security Fix: WebAuthn Assertion Verification + UseAuthentication Middleware

**Date:** 2026-04-21
**Author:** Halsey (Security/Auth)
**Status:** Implemented

## Context

Two critical authentication vulnerabilities were identified in the security review:

1. `UseAuthentication()` was missing from the ASP.NET Core middleware pipeline — cookies were issued but never validated on subsequent requests.
2. `AuthenticateWithPasskeyCommandHandler` accepted any credential ID without verifying the cryptographic assertion — no signature check, no challenge comparison.

## Decisions Made

### 1. Add `UseAuthentication()` to `Program.cs`

`app.UseAuthentication()` is now placed before `app.UseAuthorization()`. Without it, `HttpContext.User` is never populated from the `SpammaAuth` cookie, meaning all authorization policies were effectively unenforced.

### 2. WebAuthn Assertion Verification — where to verify

**Decision:** Perform cryptographic verification inside `AuthenticateWithPasskeyCommandHandler`, not at the endpoint layer.

**Rationale:** The handler is the only component that has access to the stored `PublicKey` (the attestation object CBOR) and `Algorithm`. Moving verification here keeps the security check co-located with the credential data it depends on. The endpoint constructs the command with assertion bytes and expected context values; the handler does the verification before any domain mutation.

### 3. Verifier design — injectable interface

**Decision:** `WebAuthnAssertionVerifier` implements `IWebAuthnAssertionVerifier` (internal) and is registered as a singleton. Not a static class.

**Rationale:** A static class cannot be mocked. Command handler tests need to isolate the domain logic (sign count, suspension) from cryptographic verification. Making it injectable lets existing test patterns (Moq strict mocks) work without constructing real WebAuthn payloads.

### 4. Public key format — parse from attestation object

**Decision:** The stored `PublicKey` is the raw CBOR attestation object (as registered by the browser). At verification time, the COSE public key is extracted from `authData` inside the attestation object using `System.Formats.Cbor` (in-box .NET 9).

**Rationale:** The current registration flow stores the full `attestationObject` bytes as `PublicKey`. Changing the registration storage format (to store only the COSE key) would be a data migration. Parsing at verification time is safe and defers that refactor.

**Risk noted:** If the registration flow is ever updated to store a different format, the extractor must be updated in parallel.

### 5. Verification steps implemented (per WebAuthn spec)

- `clientDataJSON.type == "webauthn.get"`
- Challenge in `clientDataJSON` matches session-stored challenge (byte-level, base64url-aware)
- `clientDataJSON.origin` matches `scheme://host:port` derived from the HTTP request
- `authenticatorData[0..31]` == `SHA-256(rpId)`
- `authenticatorData[32]` bit 0 (user-present flag) is set
- Signature verification over `authenticatorData || SHA-256(clientDataJSON)` using stored COSE key
  - ES256 (COSE alg -7): `ECDsa.VerifyData` with `HashAlgorithmName.SHA256` and `DSASignatureFormat.Rfc3279DerSequence`
  - RS256 (COSE alg -257): `RSA.VerifyData` with `HashAlgorithmName.SHA256` and `RSASignaturePadding.Pkcs1`

### 6. Error response — generic failure message

**Decision:** On any verification failure, return `PasskeyVerificationFailed` with a generic message. Do not indicate which specific check failed.

**Rationale:** Detailed failure reasons (wrong challenge vs wrong signature vs wrong origin) would help an attacker understand what to forge.

## Files Changed

| File | Change |
|------|--------|
| `src/Spamma.App/Spamma.App/Program.cs` | Added `app.UseAuthentication()` before `app.UseAuthorization()` |
| `src/modules/Spamma.Modules.UserManagement.Client/Application/Commands/PassKey/AuthenticateWithPasskeyCommand.cs` | Added `AuthenticatorData`, `ClientDataJson`, `Signature`, `ExpectedChallengeBase64`, `ExpectedOrigin`, `ExpectedRpId` |
| `src/Spamma.App/Spamma.App/Infrastructure/Endpoints/AuthenticationEndpoints.cs` | Decode assertion bytes, derive origin/rpId, pass to command |
| `src/modules/Spamma.Modules.UserManagement/Application/Services/IWebAuthnAssertionVerifier.cs` | New — verifier interface |
| `src/modules/Spamma.Modules.UserManagement/Application/Services/WebAuthnAssertionVerifier.cs` | New — CBOR parsing + ECDSA/RSA signature verification |
| `src/modules/Spamma.Modules.UserManagement/Application/CommandHandlers/Passkey/AuthenticateWithPasskeyCommandHandler.cs` | Inject `IWebAuthnAssertionVerifier`, call before domain state mutation |
| `src/modules/Spamma.Modules.UserManagement/Module.cs` | Register `WebAuthnAssertionVerifier` as singleton |
| `tests/Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/Passkey/AuthenticateWithPasskeyCommandHandlerTests.cs` | Mock `IWebAuthnAssertionVerifier`, new test for verification failure |
| `tests/Spamma.Modules.UserManagement.Tests/Application/Authorizers/Commands/Passkey/AuthenticateWithPasskeyCommandAuthorizerTests.cs` | Updated command construction |
| `tests/Spamma.Modules.UserManagement.Tests/Application/Validators/AuthenticateWithPasskeyCommandValidatorTests.cs` | Updated command construction |

## Build Status

- `Spamma.App` — ✅ 0 errors, 0 warnings
- `Spamma.Modules.UserManagement.Tests` — ✅ 0 errors, 0 warnings
- `Spamma.sln` — ❌ 75 errors in `Spamma.Modules.EmailInbox` only (pre-existing, not introduced by this work)
