# Automated test coverage review (issue #12)

## What runs

| Layer | Current coverage | CI |
| --- | --- | --- |
| DomainManagement, EmailInbox, UserManagement | Unit and PostgreSQL-backed integration tests for domain rules, handlers, authorization, repositories and projections | Push and PR |
| Spamma.App.Tests | API, component (bUnit), authentication and application configuration tests | PR; added to push CI in this change |
| SMTP E2E project | Six SMTP reception scenarios exist but all are skipped | Project runs in PR CI; scenarios remain skipped |
| Vitest | Two TypeScript files with 11 tests for setup form scripts | Added to push and PR CI in this change |
| Playwright | Five Chromium smoke tests against the running app for login, invalid-link recovery, setup lockout and anonymous inbox access | New browser workflow on PRs and main |

The .NET workflows collect Cobertura XML through `XPlat Code Coverage`. They previously searched for `.coverage` files during conversion, so the collected XML was not available as a useful artifact. Both workflows now upload the XML. A trustworthy aggregate percentage should be taken from the first green run of the updated workflows; local PostgreSQL integration tests cannot run without Docker. The browser suite needs PostgreSQL and Redis, so its CI job starts both services.

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

From `src/Spamma.App/Spamma.App`, run `npm ci`, `npm run build`, `dotnet build Spamma.App.csproj --configuration Release`, `npx playwright install chromium`, then `npm run test:e2e`. PostgreSQL and Redis must be available through `ConnectionStrings__DefaultConnection` and `ConnectionStrings__Redis`. The Playwright web server starts the app on `http://127.0.0.1:5188` with setup marked complete for an isolated test database. CI installs Chromium and uploads the Playwright report on failure.
