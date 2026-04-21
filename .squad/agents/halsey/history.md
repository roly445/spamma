# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Auth scheme: `CookieAuthenticationDefaults.AuthenticationScheme`. Cookie name: `SpammaAuth`, HttpOnly: true, SecurePolicy: SameAsRequest
- Authentication methods: magic links (primary), WebAuthn passkeys (alternative)
- Magic links: time-limited GUID tokens. Flow: email → validate token → issue `SpammaAuth` cookie with claims (UserId, email, roles)
- Passkeys: `AuthenticateWithPasskeyCommandHandler` validates assertion. WebAuthn handled in browser via `webauthn-utils.ts`
- Setup mode: `SetupDetectionService` checks `SetupSettings.Completed`. `InMemorySetupAuthService` generates random password on startup (logged at CRITICAL)
- `SetupModeMiddleware`: allows static assets always; redirects non-setup paths to `/setup-login` when setup mode enabled; blocks `/setup` paths when disabled
- Authorization: `MustBeAuthenticatedRequirement` is the standard guard. Custom policies defined per module.
- Passkey audit trail recorded via `PasskeyAuthenticated` event in `UserLookup` projection
