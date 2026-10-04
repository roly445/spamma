# Acceptance features

These files describe the intended user-visible behaviour of Spamma. They are a reviewable contract, not evidence that every scenario already works. The expectations were drafted from the current routes, UI, and existing tests; product and security decisions still need review.

| Journey | Routes or entry point | Feature | Status |
| --- | --- | --- | --- |
| Anonymous entry and access | `/`, `/login`, `/logging-in`, `/setup-login`, `/m/inbox` | [anonymous-access.feature](anonymous-access.feature) | Executable: six Playwright scenarios in CI |
| First-run setup | `/setup/*`, `/setup-login` | [setup.feature](setup.feature) | Executable .NET Playwright scenarios; ACME responses are mocked in the browser |
| Sign-in, passkey login, logout | `/login`, `/logging-in`, `/logout` | [authentication.feature](pending/authentication.feature) | Pending |
| Inbox and message viewer | `/m/inbox` | [inbox.feature](pending/inbox.feature) | Pending |
| Campaign list and details | `/m/campaigns/*` | [campaigns.feature](pending/campaigns.feature) | Pending |
| Domain administration | `/admin/domains/*` | [domains.feature](pending/domains.feature) | Pending |
| Subdomain administration | `/admin/subdomains/*` | [subdomains.feature](pending/subdomains.feature) | Pending |
| User administration | `/admin/users` | [users.feature](pending/users.feature) | Pending |
| Catch-all inbox and senders | `/m/catch-all`, `/admin/catch-all-senders` | [catch-all.feature](pending/catch-all.feature) | Pending |
| Chaos addresses | `/chaos-addresses` | [chaos-addresses.feature](pending/chaos-addresses.feature) | Pending |
| Account credentials | `/account/api-keys`, `/account/passkeys` | [account-security.feature](pending/account-security.feature) | Pending |
| Application settings and navigation access | `/admin/settings`, settings menu | [settings.feature](pending/settings.feature) | Pending |
| SMTP acceptance and routing | SMTP listener, inbox, campaigns | [smtp-reception.feature](pending/smtp-reception.feature) | Pending |

The [browser test project](../../../../../tests/Spamma.Browser.Tests/Spamma.Browser.Tests.csproj) links `anonymous-access.feature` and `setup.feature`. Reqnroll generates .NET tests from them, and C# step definitions use Playwright for .NET. CI runs setup scenarios against a first-run app backed by fresh PostgreSQL and Redis service containers, then starts the configured-app smoke run. The setup binding resets configuration rows before each scenario; the finalization scenario runs last because it disables setup mode. Certificate scenarios intercept the ACME request with controlled responses, so CI never contacts Let's Encrypt. Files under `pending/` are valid Gherkin but are not included in that project. They have no step definitions or fixtures yet, so they must not be reported as passing tests. Several pending scenarios need isolated seeded users, domains, messages, or messages; the SMTP scenarios also need an SMTP client and processing assertions.

The feature files cover the major user journeys and key success, failure, and access paths. They do not enumerate every field-validation combination or every pagination boundary. Review the expected outcomes before treating them as product requirements, particularly access isolation, setup completion, credential disclosure, and SMTP rejection behaviour.

To run the setup scenarios locally, start a fresh PostgreSQL database and Redis, then launch the published app without `Setup__Completed`. Set `ConnectionStrings__DefaultConnection`, `ConnectionStrings__Redis`, `SPAMMA_SETUP_PASSWORD`, and `DataProtection__KeysDirectory` (a writable directory). Set `SPAMMA_E2E_BASE_URL` and `SPAMMA_E2E_RESET_SETUP_CONFIG=true` for the browser tests. Run `dotnet test tests/Spamma.Browser.Tests/Spamma.Browser.Tests.csproj --filter "Category=setup&Category!=setup-finalize"`, then run with `--filter "Category=setup-finalize"`. Playwright normally uses its installed Chromium; `SPAMMA_E2E_CHROMIUM_PATH` can point to a local Chromium-compatible browser such as Edge when the bundled browser is unavailable. Use a disposable database: the setup scenarios reset and seed configuration rows.
