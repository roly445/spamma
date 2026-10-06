# Automated test coverage review (issue #12)

## What runs

| Layer | Current coverage | CI |
| --- | --- | --- |
| DomainManagement, EmailInbox, UserManagement | Unit and PostgreSQL-backed integration tests for domain rules, handlers, authorization, repositories and projections | Push and PR |
| Spamma.App.Tests | API, component (bUnit), authentication and application configuration tests | PR; added to push CI in this change |
| EmailInbox E2E project | Six SMTP reception, three durability, and two authenticated gRPC scenarios use disposable PostgreSQL; SMTP scenarios also use Redis | Project runs in PR CI with no skipped scenarios |
| Vitest | Two TypeScript files with 11 tests for setup form scripts | Added to push and PR CI in this change |
| Playwright | Six Chromium anonymous-access scenarios against the running app for login, invalid email input, missing-token recovery, setup lockout and anonymous inbox access, plus the first-run setup scenarios | Browser workflow on PRs and main |

The browser scenarios are written in [`anonymous-access.feature`](../src/Spamma.App/Spamma.App/e2e/features/anonymous-access.feature). Reqnroll generates .NET tests from that file, and the C# bindings use Playwright for .NET to control Chromium. These are proposed acceptance expectations inferred from current application behaviour, not proof that the behaviour is what users want. In particular, review the generic response for an unregistered address and the setup and inbox access rules. The missing-token scenario does not claim to exercise an invalid signed token. The authenticated login and inbox journey remains a separate gap below.

The [acceptance feature index](../src/Spamma.App/Spamma.App/e2e/features/README.md) now maps the remaining UI and SMTP journeys to feature files. Those files are tagged `@pending` and are not included in the browser test project until fixtures and C# step definitions exist; their presence documents expected behaviour but does not increase automated test coverage.

The .NET workflows collect Cobertura XML through `XPlat Code Coverage`. They previously searched for `.coverage` files during conversion, so the collected XML was not available as a useful artifact. Both workflows now upload the XML, and `Spamma.App.Tests` now includes the collector. The first PR run produced these per-suite line rates before the App collector was added: DomainManagement 951/8,104 (11.7%); EmailInbox 1,386/5,803 (23.9%); UserManagement 1,113/2,370 (47.0%); and the skipped SMTP E2E suite 189/8,067 (2.3%, largely fixture startup). Each report includes referenced assemblies, so these rates cannot be added into one repository percentage. Local PostgreSQL integration tests cannot run without Docker. The browser suite needs PostgreSQL and Redis, so its CI job starts both services.

## Highest-risk gaps

| Priority | Gap | Recommended next test | Effort / owner |
| --- | --- | --- | --- |
| 1 | No authenticated browser journey through magic-link login, inbox, email inspection and campaign tabs | Seed two scoped users and captured messages in an isolated browser fixture, then assert visible data and cross-user isolation through the real UI | Medium / App + UserManagement + EmailInbox |
| 2 | The new browser suite covers anonymous flows but not domain management or administrative permissions | Extend the seeded browser fixture with a domain/subdomain and two users, then test allowed and denied navigation and actions | Medium / App + DomainManagement |
| 3 | The HTML/TypeScript UI has only two Vitest files | Add focused tests for form and viewer scripts when behavior changes; use Playwright for interactions that require the browser and server | Low / App |

## SMTP and gRPC service tests

All six cases in `SmtpEmailReceptionTests` run through the real SMTP server and CAP publisher/subscriber with isolated PostgreSQL and Redis containers. The fixture seeds a verified domain, starts SMTP on a free local port, and polls the persisted projections with a 40-second bound. The cases cover accepted and rejected mail, enabled and disabled chaos addresses, campaign metadata, and concurrent delivery. Three `SmtpDurabilityTests` cases verify CAP persistence before SMTP acknowledgment, recovery by a second host, retry without double counting a campaign, and inspectable failure details after retry exhaustion. Two `GrpcApiKeyLifecycleTests` cases call `GetEmailContent` through an HTTP/2 test server with a real PostgreSQL-backed API-key repository; they prove that a key works before revocation or expiry and returns `Unauthenticated` afterward. Run all eleven locally with Docker available using `dotnet test tests/Spamma.Modules.EmailInbox.Tests.E2E/Spamma.Modules.EmailInbox.Tests.E2E.csproj -c Release`.

## Running the new browser suite

Build the frontend with `npm ci` and `npm run build` from `src/Spamma.App/Spamma.App`. From the repository root, build the app and browser test project in Release mode, then run `pwsh tests/Spamma.Browser.Tests/bin/Release/net10.0/playwright.ps1 install chromium`. PostgreSQL and Redis must be available through `ConnectionStrings__DefaultConnection` and `ConnectionStrings__Redis`. Start the app on `http://127.0.0.1:5188` with `Setup__Completed=2026-01-01T00:00:00Z` and `SmtpServer__Port=2526`, then run `dotnet test tests/Spamma.Browser.Tests/Spamma.Browser.Tests.csproj --configuration Release --no-build`. CI performs these steps and uploads the .NET test results, failure traces, and app log on failure.
