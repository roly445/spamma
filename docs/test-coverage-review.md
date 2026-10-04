# Automated test coverage review (issue #12)

## What runs

| Layer | Current coverage | CI |
| --- | --- | --- |
| DomainManagement, EmailInbox, UserManagement | Unit and PostgreSQL-backed integration tests for domain rules, handlers, authorization, repositories and projections | Push and PR |
| Spamma.App.Tests | API, component (bUnit), authentication and application configuration tests | PR; added to push CI in this change |
| SMTP E2E project | Six SMTP reception scenarios exist but all are skipped | Project runs in PR CI; scenarios remain skipped |
| Vitest | Two TypeScript files with 11 tests for setup form scripts | Added to push and PR CI in this change |
| Playwright | Five Chromium scenarios against the running app for login, missing-token recovery, setup lockout and anonymous inbox access | New browser workflow on PRs and main |

The browser scenarios are written in [`anonymous-access.feature`](../src/Spamma.App/Spamma.App/e2e/features/anonymous-access.feature). Reqnroll generates .NET tests from that file, and the C# bindings use Playwright for .NET to control Chromium. These are proposed acceptance expectations inferred from current application behaviour, not proof that the behaviour is what users want. In particular, review the generic response for an unregistered address and the setup and inbox access rules. The missing-token scenario does not claim to exercise an invalid signed token. The authenticated login and inbox journey remains a separate gap below.

The [acceptance feature index](../src/Spamma.App/Spamma.App/e2e/features/README.md) now maps the remaining UI and SMTP journeys to feature files. Those files are tagged `@pending` and are not included in the browser test project until fixtures and C# step definitions exist; their presence documents expected behaviour but does not increase automated test coverage.

The .NET workflows collect Cobertura XML through `XPlat Code Coverage`. They previously searched for `.coverage` files during conversion, so the collected XML was not available as a useful artifact. Both workflows now upload the XML, and `Spamma.App.Tests` now includes the collector. The first PR run produced these per-suite line rates before the App collector was added: DomainManagement 951/8,104 (11.7%); EmailInbox 1,386/5,803 (23.9%); UserManagement 1,113/2,370 (47.0%); and the skipped SMTP E2E suite 189/8,067 (2.3%, largely fixture startup). Each report includes referenced assemblies, so these rates cannot be added into one repository percentage. Local PostgreSQL integration tests cannot run without Docker. The browser suite needs PostgreSQL and Redis, so its CI job starts both services.

## Highest-risk gaps

| Priority | Gap | Recommended next test | Effort / owner |
| --- | --- | --- | --- |
| 1 | No authenticated browser journey through magic-link login, inbox, email inspection and campaign tabs | Seed two scoped users and captured messages in an isolated browser fixture, then assert visible data and cross-user isolation through the real UI | Medium / App + UserManagement + EmailInbox |
| 1 | Six skipped SMTP E2E scenarios leave SMTP-to-storage and campaign processing untested end to end | Repair the fixture with Redis and CAP registration, wait on persisted projections instead of fixed delays, then enable acceptance, rejection and campaign cases first | Medium / EmailInbox |
| 2 | Durable SMTP acceptance is covered by mocked publish/storage failures and payload serialization, without a process-restart integration test | Publish through CAP to PostgreSQL, restart the host, and assert the subscriber processes the retained MIME payload once | Medium / EmailInbox |
| 2 | The new browser suite covers anonymous flows but not domain management or administrative permissions | Extend the seeded browser fixture with a domain/subdomain and two users, then test allowed and denied navigation and actions | Medium / App + DomainManagement |
| 3 | The HTML/TypeScript UI has only two Vitest files | Add focused tests for form and viewer scripts when behavior changes; use Playwright for interactions that require the browser and server | Low / App |

## Skipped SMTP tests

All six cases in `SmtpEmailReceptionTests` have the same skip reason: projections are not running correctly, with a reference to `E2E_TEST_STATUS.md`, which is absent. The fixture currently starts PostgreSQL and the modules, but does not configure the Redis/CAP infrastructure that now owns SMTP capture. It also uses fixed `Task.Delay` calls to await processing. The owner should add a Redis test container and CAP configuration, verify projection startup, replace sleeps with bounded condition polling, and remove skips one scenario at a time. The remaining cases cover unknown domains, chaos addresses, campaign headers and concurrent delivery.

## Running the new browser suite

Build the frontend with `npm ci` and `npm run build` from `src/Spamma.App/Spamma.App`. From the repository root, build the app and browser test project in Release mode, then run `pwsh tests/Spamma.Browser.Tests/bin/Release/net10.0/playwright.ps1 install chromium`. PostgreSQL and Redis must be available through `ConnectionStrings__DefaultConnection` and `ConnectionStrings__Redis`. Start the app on `http://127.0.0.1:5188` with `Setup__Completed=2026-01-01T00:00:00Z` and `SmtpServer__Port=2526`, then run `dotnet test tests/Spamma.Browser.Tests/Spamma.Browser.Tests.csproj --configuration Release --no-build`. CI performs these steps and uploads the .NET test results, failure traces, and app log on failure.
