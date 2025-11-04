Proposed remediation patches (preview)

This file lists concrete patch edits to be applied to the feature spec and tasks for `specs/001-email-campaigns`.
These are proposals only — they do not change any repository files until you say "apply".

Summary of changes
- Deduplicate FRs in `spec.md` (merge FR-003, FR-011 duplicates and FR-015 duplicates).
- Add new tasks to `tasks.md` for critical missing items:
  - T031: Auto-delete exclusion for campaign-tagged messages (prevent RO read-model / backfill deletions)
  - T032: Implement Delete Campaign API (.Client DTO + server handler + audit event)
  - T033: Create export `.Client` DTO + server export handler with RBAC and rate-limiting
  - T034: Add RBAC enforcement tasks for viewing campaign summaries and exports
  - T035: Add frontend task to include ApexCharts dependency and bundle steps and verify headless build
  - T036: Add CI task to run headless frontend build and install apexcharts (npm ci + build) in pipeline

Patches (diff-like, human readable)

*** Update File: specs/001-email-campaigns/spec.md
@@
- FR-003: The system must collect counts of messages per campaign and the timestamps of the first and last observed message. Counts must be precise to the message and updated in <unspecified window>.
+ FR-003: The system must collect counts of messages per campaign and the timestamps of the first and last observed message. Counts must be precise to the message and updated within an operator-configurable window (default: 30s). Manual refresh is available via the UI (see FR-018).
@@
- FR-011: The system must support deleting all messages associated with a campaign via an admin API. Deleting messages must produce an audit trail.
+ FR-011: The system must support deleting all messages associated with a campaign via an admin API. Deleting messages must produce an audit trail, accept a `force` flag for hard-delete, and must be protected by RBAC (Admin role).
@@
- FR-015: The system must support exporting campaign data as CSV and JSON. Exports larger than 10,000 rows must be performed asynchronously and delivered via email or stored in the IO store.
+ FR-015: The system must support exporting campaign data as CSV and JSON. Exports larger than the synchronous threshold (default: 10,000 rows) must be performed asynchronously and delivered via the configured export delivery (email attachment or object store). Exports require proper authorization (see FR-013).
@@
+ New FR: FR-016 (clarified): Auto-delete and retention rules must not automatically delete messages tagged with a campaign unless the deletion is explicitly excluded. When an auto-delete job runs, it must check for campaign-tagging and skip deletion unless a campaign-delete operation is explicitly requested (via FR-011).

*** Update File: specs/001-email-campaigns/tasks.md
@@
+ T031: Implement auto-delete exclusion for campaign-tagged messages.
+ - Location: src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/AutoDeleteService.cs (or similar)
+ - Actions:
+   - Update auto-delete job logic to query CampaignSummary/Index to determine message campaign membership before deletion.
+   - Add unit tests verifying that messages with campaign tag are not deleted by default.
+   - Add integration test: ingest message with x-spamma-camp, run auto-delete job, assert message remains.
+
+ T032: Add Delete Campaign API and `.Client` DTOs.
+ - Location: src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/
+ - Actions:
+   - Add `DeleteCampaignCommand.cs` (BluQubeCommand Path = "api/email-inbox/campaigns/delete").
+   - Add server-side handler `DeleteCampaignCommandHandler` which:
+       - Validates RBAC (Admin), records an audit event, and invokes repository deletion (or marks for deletion if soft-delete).
+       - Ensures safe deletion (checks retention policy overrides and does not delete unrelated streams).
+   - Add unit tests and integration test covering audit publish and repository delete.
+
+ T033: Add Export Campaign Data `.Client` DTO and Server Export Handler.
+ - Location: src/modules/Spamma.Modules.EmailInbox.Client/Application/Queries/ and Server Application/QueryProcessors/
+ - Actions:
+   - Add `ExportCampaignDataQuery.cs` in .Client with BluQubeQuery Path "api/email-inbox/campaigns/export" and associated `ExportCampaignDataQueryResult`.
+   - Server-side query processor performs synchronous export up to threshold; for larger exports, create an ExportJob (async) that stores result in IO store and sends notification.
+   - Add rate-limiting and RBAC checks (see T034).
+
+ T034: RBAC enforcement for campaign viewing and exports.
+ - Location: src/modules/Spamma.Modules.EmailInbox/Application/Authorizers/
+ - Actions:
+   - Add requirements: `MustBeAuthenticatedRequirement` for read APIs; `MustBeAdminRequirement` for delete/export operations.
+   - Add unit tests verifying authorization behavior and API-level protection.
+
+ T035: Frontend ApexCharts integration task.
+ - Location: src/Spamma.App/Spamma.App.Client/Assets/
+ - Actions:
+   - Add `apexcharts` npm dependency (package.json), integrate via existing webpack build.
+   - Create `CampaignsChart.razor` / `CampaignsChart.ts` wrapper to render Brush chart with Tailwind styles and header `x-spamma-camp` in datapoint labeling.
+   - Add visual unit/UI tests to render chart server-side in headless mode and verify build artifacts.
+
+ T036: CI update for headless frontend build and apexcharts.
+ - Location: .github/workflows/ci.yml (or similar)
+ - Actions:
+   - Ensure `npm ci` and `npm run build` are run during CI for Spamma.App.Client assets.
+   - Add caching for node_modules and verify headless Webpack build works in CI environment.
+
Notes and rationale
- These tasks map exactly to the CRITICAL and HIGH findings from the cross-artifact analysis. The auto-delete exclusion is CRITICAL because failing to implement it causes potential data loss and violates retention guarantees.
- Adding `.Client` DTOs ensures BluQube codegen can generate endpoints for WASM clients and keeps the project consistent with the constitution rules.
- RBAC tasks ensure exports and deletes are only performed by authorized users.

What I will do next
- If you approve, I will apply these patches to the repository (update the files directly). After applying, I will run a quick repo build to ensure no compile errors and list any issues.
- If you'd prefer, I can produce a proper git patch/PR instead of applying directly.

End of proposed remediation patches
