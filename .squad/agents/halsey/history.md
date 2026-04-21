# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- WebAuthn assertion verification is implemented in `WebAuthnAssertionVerifier` (injectable via `IWebAuthnAssertionVerifier`). Checks: type="webauthn.get", challenge match (byte-level, base64url decode), origin match, rpIdHash match (SHA-256), user-present flag, ECDSA/RSA signature over `authenticatorData || SHA-256(clientDataJSON)`.
- Stored `PublicKey` on passkey is the full CBOR attestation object (not just the COSE key). COSE key is extracted from `authData` at verification time via `System.Formats.Cbor`. Format depends on registration not being changed.
- `UseAuthentication()` must appear before `UseAuthorization()` in the middleware pipeline — without it HttpContext.User is never populated.
- `WebAuthnAssertionVerifier` is a singleton service (not static) to support test mocking via `IWebAuthnAssertionVerifier`.
- Auth scheme: `CookieAuthenticationDefaults.AuthenticationScheme`. Cookie name: `SpammaAuth`, HttpOnly: true, SecurePolicy: SameAsRequest
- Authentication methods: magic links (primary), WebAuthn passkeys (alternative)
- Magic links: time-limited GUID tokens. Flow: email → validate token → issue `SpammaAuth` cookie with claims (UserId, email, roles)
- Passkeys: `AuthenticateWithPasskeyCommandHandler` validates assertion. WebAuthn handled in browser via `webauthn-utils.ts`
- Setup mode: `SetupDetectionService` checks `SetupSettings.Completed`. `InMemorySetupAuthService` generates random password on startup (logged at CRITICAL)
- `SetupModeMiddleware`: allows static assets always; redirects non-setup paths to `/setup-login` when setup mode enabled; blocks `/setup` paths when disabled
- Authorization: `MustBeAuthenticatedRequirement` is the standard guard. Custom policies defined per module.
- Passkey audit trail recorded via `PasskeyAuthenticated` event in `UserLookup` projection

### Security Review Findings — 2026-04-21

- 🔴 **CRITICAL: WebAuthn assertion signature is never verified.** `AuthenticateWithPasskeyCommandHandler` only checks credential ID exists and sign count — the `Signature` from `AssertionResponseData` is received but ignored. Stored `PublicKey` is never used. Anyone with a valid credential ID can authenticate without the private key. `Fido2NetLib` integration needed.
- 🔴 **CRITICAL: WebAuthn challenge is never verified.** The session challenge is retrieved in `MakeAssertion` but never compared to the challenge in `clientDataJSON`. Replay attacks are possible.
- 🟡 **Setup password logged in plaintext** — `ValidatePassword()` logs the submitted password in warning messages. Remove `Password={Password}` from all log calls in `InMemorySetupAuthService`.
- 🟡 **`GetCurrentSetupPassword()` exposes raw password** — public method on `IInMemorySetupAuthService` returns plain-text setup password. Should be removed.
- 🟡 **Setup password has ~16 bits entropy** — 8×8×900 = 57,600 possible values. No brute-force protection on `/setup-login`. Increase entropy and add lockout.
- 🟡 **`SetupModeMiddleware` uses `path.Contains()`** — allow-list matching uses `Contains` not `StartsWith`/extension check. Paths like `/evil.js.payload` bypass protection. `/api/` blanket bypass is also very broad.
- 🟡 **`UseAuthentication()` is missing from `Program.cs`** — only `UseAuthorization()` is in the pipeline. Cookie middleware may not populate `HttpContext.User` for minimal API endpoints.
- 🟡 **Raw API key used as Redis cache key** in `ApiKeyValidationService` — `$"api_key_validation:{apiKey}"` stores the key in Redis. `ApiKeyRateLimiter` correctly hashes; validation service should too.
- 🟡 **Hardcoded credentials in `appsettings.Development.json`** — DB password `marten_password` and cert password `m05sAzv7IiMw0XGD` committed to source.
- 🟡 **OTEL trace forwarding endpoint has no auth and no request size limit** — `/api/otel/traces` is an unauthenticated DoS vector and potential SSRF.
- Magic link tokens are JWTs: HMAC-SHA256, 1-hour expiry, signing key from database. `ValidateIssuer = false, ValidateAudience = false` — acceptable but not ideal.
- `IInternalQueryStore` uses `ConditionalWeakTable` keyed on object identity to bypass authorization for server-internal queries. Pattern is safe but non-obvious.
- `ReceivedEmailCommand` authorizer is intentionally empty — only called from SMTP processing stack, not HTTP endpoints.
