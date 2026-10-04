# Acceptance features

These files describe the intended user-visible behaviour of Spamma. They are a reviewable contract, not evidence that every scenario already works. The expectations were drafted from the current routes, UI, and existing tests; product and security decisions still need review.

| Journey | Routes or entry point | Feature | Status |
| --- | --- | --- | --- |
| Anonymous entry and access | `/`, `/login`, `/logging-in`, `/setup-login`, `/m/inbox` | [anonymous-access.feature](anonymous-access.feature) | Executable: five Playwright scenarios in CI |
| First-run setup | `/setup/*`, `/setup-login` | [setup.feature](pending/setup.feature) | Pending |
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

The [browser test project](../../../../../tests/Spamma.Browser.Tests/Spamma.Browser.Tests.csproj) links `anonymous-access.feature`. Reqnroll generates .NET tests from it, and C# step definitions use Playwright for .NET. Files under `pending/` are valid Gherkin but are not included in that project. They have no step definitions or fixtures yet, so they must not be reported as passing tests. Promote a scenario by including its feature file in the .NET test project, implementing its C# steps and fixture, and running it in CI. Several pending scenarios need isolated seeded users, domains, messages, or a first-run app; the SMTP scenarios also need an SMTP client and processing assertions.

The feature files cover the major user journeys and key success, failure, and access paths. They do not enumerate every field-validation combination or every pagination boundary. Review the expected outcomes before treating them as product requirements, particularly access isolation, setup completion, credential disclosure, and SMTP rejection behaviour.
