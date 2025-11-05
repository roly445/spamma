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

## User-reported issues (merged)

The user provided a list of known issues to merge into this scan report. I recorded them below with a suggested priority and action for each.

1. RefreshableAuthenticationStateProvider exists but is unused.

  - File area: Authentication / client-side provider.
  - Suggested priority: P2 — low-risk cleanup / correctness.
  - Suggested action: Wire up `RefreshableAuthenticationStateProvider` where authentication state is refreshed (e.g., on navigation or periodically), or remove and consolidate if unused.

2. Emails that are part of campaigns can be deleted manually; they shouldn't be — only deleted when a campaign is deleted.

  - File area: Email inbox / campaign handling / repositories.
  - Suggested priority: P1 — data-loss risk for campaigns.
  - Suggested action: Disallow manual deletion of emails that belong to a campaign in the UI and enforce at the API/repository level (validation or foreign-key-like check), add tests.

3. The navigation menu (via the cog) does not close when an item is clicked.

  - File area: Client UI — navigation components / app assets.
  - Suggested priority: P2 — UX annoyance.
  - Suggested action: Ensure click handlers close the menu after navigation (invoke existing remove/close logic) and add an integration UI test where possible.

4. Modal and slideout backgrounds are a solid colour; they should have some transparency.

  - File area: Assets / styles (Tailwind/SCSS) for overlays.
  - Suggested priority: P3 — visual polish.
  - Suggested action: Update overlay background CSS to use an rgba or utility class for semi-transparent bg (e.g., bg-black/50) and ensure consistent across components.

5. Modals don't share a common base or style so they look slightly different.

  - File area: Client components / modal implementations.
  - Suggested priority: P3 — visual consistency.
  - Suggested action: Create a shared `ModalBase` component (razor) or CSS class and migrate modal instances to use the shared base.

6. Slideouts don't share a common base or style so they look slightly different.

  - File area: Client components / slideout implementations.
  - Suggested priority: P3 — visual consistency.
  - Suggested action: Create a shared `SlideoutBase` or common CSS and migrate existing slideouts.

7. Slideouts have no open/close animation.

  - File area: UI animations for slideouts.
  - Suggested priority: P3 — UX polish.
  - Suggested action: Add CSS transitions for transform/opacity and use a shared toggling class to animate in/out.

8. Modals and slideouts should not close on background click.

  - File area: modal/slideout behavior.
  - Suggested priority: P2 — interaction correctness (some flows require explicit close)
  - Suggested action: Disable background-click-to-close behavior or make it opt-in; ensure confirm dialogs remain strict.

9. Chart on the campaign view is no longer needed.

  - File area: Campaign view component.
  - Suggested priority: P3 — removal/refactor.
  - Suggested action: Remove chart component and tighten layout, update any tests or snapshots.

10. On the campaign listing, the filter card is set up for 4 items but there are only 3 items in it.

  - File area: Campaign listing UI / filter card.
  - Suggested priority: P3 — minor UI fix.
  - Suggested action: Adjust layout/count to match the actual filters or add the missing filter if intended.

11. Emails in a campaign should not be favourited.

  - File area: Email list / campaign UI and command handling.
  - Suggested priority: P1 — data/UX policy.
  - Suggested action: Disable the favourite action for campaign-bound emails in the UI and enforce at API/command level so favouriting is rejected for campaign emails.

If you want, I can begin implementing the P1 items from this merged list now (items 2 and 11), or continue the automated scan and produce line-level findings for the P2/P3 items so we can create focused PRs. Which do you prefer?
