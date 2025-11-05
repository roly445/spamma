 # Scan report — 001-bug-sweep

 Generated: 2025-11-05 (local session)

 ## Summary

 This report collects candidate locations in the repository that look like small bugs, rough edges, or "niggles" worth addressing in the 001-bug-sweep. The scan is source-focused and excludes build artifacts (`bin/`, `obj/`) and framework bundles under `wwwroot/_framework`.

 ## Priority legend

 - P1 — Likely to cause runtime failures or obvious missing behavior; fix first.
 - P2 — User-visible but low-risk (logging, noisy console output, unclear error messages).
 - P3 — Cosmetic or defensive improvements (message clarity, minor refactors).

 ## P1 candidates

 1) DomainValidationService.IsSubdomainValid — NotImplementedException

 - File: `src/Spamma.App/Spamma.App.Client/Infrastructure/Services/DomainValidationService.cs`
 - Symptom: `public bool IsSubdomainValid(string domain) { throw new NotImplementedException(); }`
 - Why fix: callers that need to validate subdomains will encounter NotImplementedException at runtime. This is a clear bug and should be implemented or guarded.
 - Suggested fix: implement parity with `IsDomainValid` using `DomainParser` and check that the input is a proper subdomain of the registrable domain (not equal to the registrable domain itself). Consider trimming input and handling null/empty strings.
 - Effort: small — ~15-40 minutes to implement and run a quick build + tests.

 ## P2 candidates (initial)

 The filtered scan located multiple client-side console logging occurrences and a set of throw patterns worth triage. Below are curated examples with suggested actions.

 1) Client-side console.* usage (noisy/diagnostic logging)

 - Files & notable lines (examples found during the filtered scan):
   - `src/Spamma.App/Spamma.App/Assets/Scripts/app.ts` — `console.log('App TypeScript loaded');` (line ~4) and `console.error('Error invoking .NET method from outside click handler', err);` (line ~37).
   - `src/Spamma.App/Spamma.App/wwwroot/setup-email.js` / `Assets/Scripts/setup-email.ts` — `console.warn('Unknown SMTP provider: ...')` and `console.warn('SMTP form elements not found')`.
   - `src/Spamma.App/Spamma.App/wwwroot/setup-certificates.js` / `Assets/Scripts/setup-certificates.ts` — several `console.log` / `console.error` calls used for streaming progress and error reporting.
   - `src/Spamma.App/Spamma.App/Assets/Scripts/webauthn-utils.ts` — `console.error('Registration error:', error)` and `console.error('Authentication error:', error)`.

 - Why fix: these logs are useful in development but can be noisy in production browsers and may leak internal details. Replace with a structured logging mechanism (send diagnostics to server when appropriate) or gate behind a debug flag.
 - Suggested action: audit each occurrence, remove or lower log level, or replace with calls to a lightweight client logger that can be toggled.
 - Effort: small — ~1–2 hours to audit and adjust.

 2) Throw patterns to review (argument / invalid-operation guards)

 - Files & examples:
   - `src/Spamma.App/Spamma.App.Client/Infrastructure/Helpers/Extensions.cs` — `throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");`
   - `src/modules/Spamma.Modules.EmailInbox/Infrastructure/IntegrationEventHandlers/PersistReceivedEmailHandler.cs` — `throw new InvalidOperationException($"Failed to persist email {ev.EmailId}");` and `throw new InvalidOperationException($"Failed to process received email {ev.EmailId}", ex);`
   - `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/CertesLetsEncryptService.cs` — `throw new InvalidOperationException($"Failed to generate PFX certificate for domain {domain}", ex);`
   - Multiple aggregate event switch guards: `throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");` — these are defensive checks but should be reviewed for clarity and test coverage.

 - Why fix/review: most of these are deliberate defensive checks. Triage to ensure they are not surfaced directly to end users and that they have appropriate tests. Improve messages where helpful.
 - Effort: medium — initial triage ~1 hour, fixes depend on findings.

 ## Scan plan and next steps

 1. Re-run (or expand) filtered scans if you want broader coverage (exclude `**/bin/**`, `**/obj/**`, and `**/wwwroot/_framework/**`) and add any hits to this report.
 2. Create prioritized tasks (issues or cards) and implement small PRs starting with P1 candidate(s). Add tests where useful.

 ## User input requested

 Do you want me to implement the P1 change (`IsSubdomainValid`) now (apply patch + run build/tests), or leave it in the report and continue scanning for more candidates first?
