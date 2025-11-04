# Implementation Tasks: Email campaigns

Phase 1: Setup

- [ ] T001 Initialize feature plan artifacts in specs/ (spec.md, plan.md, research.md, data-model.md, tasks.md)
- [ ] T002 [P] Ensure CI can build Blazor client headless (update pipeline if needed) - .github/workflows/
- [ ] T003 Add configuration keys for CampaignCaptureConfig to shared settings - src/modules/Spamma.Modules.Common/Settings.cs

Phase 2: Foundational (blocking prerequisites)

- [ ] T004 [P] [US1] Add `CampaignSummary` read model document and Marten projection skeleton - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/CampaignSummaryProjection.cs
- [ ] T005 [P] [US1] Add `CampaignSample` document and local message store provider interface implementation stub - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/LocalMessageStoreProvider.cs
- [ ] T006 [P] Add configuration for header name `x-spamma-camp` and max header length - src/modules/Spamma.Modules.Common/Settings/CampaignCaptureConfig.cs
- [ ] T007 [P] [US1] Add CQRS command DTO `RecordCampaignCaptureCommand` in EmailInbox.Client - src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/RecordCampaignCaptureCommand.cs

Phase 3: User Story 1 - View campaign list (Priority: P1)

- [ ] T008 [US1] Implement query `GetCampaignsQuery` DTO in DomainManagement.Client - src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetCampaignsQuery.cs
- [ ] T009 [US1] Implement query processor `GetCampaignsQueryProcessor` to read `CampaignSummary` projections - src/modules/Spamma.Modules.DomainManagement/Application/QueryProcessors/GetCampaignsQueryProcessor.cs
- [ ] T010 [US1] Add Campaigns page UI skeleton (razor + code-behind) and list component - src/Spamma.App/Spamma.App.Client/Pages/Campaigns.razor, Campaigns.razor.cs
- [ ] T011 [US1] Add paging, sorting, and filtering UI and wire to `IQuerier` calls - src/Spamma.App/Spamma.App.Client/Pages/Campaigns.razor.cs
- [ ] T012 [US1] Add manual "Refresh" control and wire to re-query backend - src/Spamma.App/Spamma.App.Client/Components/Campaigns/RefreshButton.razor

Phase 4: User Story 2 - Campaign drill-in & sample (Priority: P1)

- [ ] T013 [US2] Implement query `GetCampaignDetailQuery` DTO - src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetCampaignDetailQuery.cs
- [ ] T014 [US2] Implement query processor `GetCampaignDetailQueryProcessor` returning time-series buckets and sample - src/modules/Spamma.Modules.DomainManagement/Application/QueryProcessors/GetCampaignDetailQueryProcessor.cs
- [ ] T015 [US2] Add Campaign detail UI with ApexCharts brush timeline and sample display - src/Spamma.App/Spamma.App.Client/Pages/CampaignDetail.razor, CampaignDetail.razor.cs
- [ ] T016 [US2] Integrate ApexCharts via JS interop wrapper and Tailwind-friendly container - src/Spamma.App/Spamma.App.Client/Assets/Scripts/apexcharts-wrapper.ts and wwwroot assets
- [ ] T017 [US2] Add UI CTA to open sample message in inbox viewer and wire navigation - src/Spamma.App/Spamma.App.Client/Components/Campaigns/SamplePreview.razor

Phase 5: User Story 3 - Inbox tagging & journey (Priority: P2)

- [ ] T018 [US3] Update inbox message list rendering to include campaign badge when header present - src/Spamma.App/Spamma.App.Client/Components/Inbox/MessageRow.razor
- [ ] T019 [US3] Add CTA in message viewer linking to campaign detail - src/Spamma.App/Spamma.App.Client/Components/Inbox/MessageViewer.razor

Phase 6: Capture & Projection Implementation (behind User Story flows)

- [ ] T020 [US1] Update SMTP ingestion to detect `x-spamma-camp` header, normalize value to lower-case, and publish `RecordCampaignCaptureCommand` - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/SpammaMessageStore.cs
- [ ] T021 [US1] Implement `RecordCampaignCaptureCommandHandler` to update `CampaignSummary` projection and create `CampaignSample` when first-seen - src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/RecordCampaignCaptureCommandHandler.cs
- [ ] T022 [US1] Ensure receipt/audit event is logged synchronously at ingress (lightweight) - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Logging/SmtpAuditLogger.cs
- [ ] T023 [US1] Add retry/backoff for async persistence jobs (use CAP or background workers) - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Workers/CaptureRetryWorker.cs

Phase 7: Export & Admin

- [ ] T024 [US1] Implement export endpoint/command using BluQube command with CSV/JSON formats and synchronous limit enforcement - src/modules/Spamma.Modules.DomainManagement/Application/CommandHandlers/ExportCampaignsCommandHandler.cs
- [ ] T025 [US1] Audit and rate-limit exports (log requests, add throttling) - src/modules/Spamma.Modules.DomainManagement/Infrastructure/ExportAuditService.cs

Phase 8: Tests & CI

- [ ] T026 [US1] Add unit tests for header normalization and validation - tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/SpammaMessageStoreTests.cs
- [ ] T027 [US1] Add integration test for end-to-end SMTP -> capture -> projection -> query - tests/Spamma.Modules.EmailInbox.Tests/Integration/SmtpCaptureIntegrationTests.cs
- [ ] T028 [US1] Add UI test (minimal) to ensure Campaigns page renders and manual refresh triggers backend call (if UI test infra exists) - tests/Spamma.App.Tests/Client/CampaignsPageTests.cs

Polish & Cross-cutting

- [ ] T029 Finalize observability: metrics for captures/sec, projection-lag, export counts - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Observability/CampaignMetrics.cs
- [ ] T030 Documentation: update quickstart.md and README with campaign feature instructions - specs/001-email-campaigns/quickstart.md

Additional Critical Tasks (remediation additions)

- [ ] T031 [P] Auto-delete exclusion: Implement auto-delete exclusion for campaign-tagged messages.
	- Location: src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/AutoDeleteService.cs
	- Actions:
		- Update auto-delete job to query CampaignSummary/Index and skip deletion of messages that belong to a campaign unless an explicit campaign-delete request is provided.
		- Add unit test verifying messages with campaign tag are not deleted by the standard auto-delete flow.
		- Add integration test: ingest message with `x-spamma-camp`, run auto-delete job, assert message remains.

- [ ] T032 [P] Delete Campaign API + `.Client` DTOs and server handler.
	- Location: src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/DeleteCampaignCommand.cs
	- Actions:
		- Add `DeleteCampaignCommand` (BluQubeCommand Path = "api/email-inbox/campaigns/delete").
		- Implement `DeleteCampaignCommandHandler` server-side; validate RBAC (Admin / `CanModerationChaosAddressesRequirement`) and record an audit event. Support `force` (hard-delete) and `soft` modes.
		- Add unit and integration tests for audit and repository delete behavior.

- [ ] T033 [P] Export Campaign Data `.Client` DTO + Server Export Handler with RBAC and async fallback.
	- Location: src/modules/Spamma.Modules.EmailInbox.Client/Application/Queries/ExportCampaignDataQuery.cs
	- Actions:
		- Add `ExportCampaignDataQuery` (BluQubeQuery Path = "api/email-inbox/campaigns/export") and result DTO.
		- Server-side: implement synchronous export up to threshold; for larger exports, enqueue async export job that stores output in IO store and notifies requester.
		- Enforce authorization and implement rate-limiting hooks.

- [ ] T034 RBAC enforcement for campaign viewing and exports.
	- Location: src/modules/Spamma.Modules.EmailInbox/Application/Authorizers/
	- Actions:
		- Add `MustBeAdminRequirement` and wiring where needed; reuse existing `CanModerationChaosAddressesRequirement` semantics for moderation privileges.
		- Protect delete/export endpoints and add tests for forbidden/allowed access.

- [ ] T035 Frontend: Add ApexCharts dependency and chart wrapper.
	- Location: src/Spamma.App/Spamma.App.Client/Assets/
	- Actions:
		- Add `apexcharts` to package.json and include in webpack build.
		- Implement `CampaignsChart.razor` + `CampaignsChart.razor.cs` and a small TS wrapper `apexcharts-wrapper.ts` to render the Brush chart with Tailwind-consistent styling and the `x-spamma-camp` header shown in labels.
		- Add visual smoke test to assert chart bundle builds in headless mode.

- [ ] T036 CI: Ensure headless frontend build and apexcharts are built in CI.
	- Location: .github/workflows/ci.yml
	- Actions:
		- Add `npm ci` and `npm run build` steps for `Spamma.App.Client` assets in CI.
		- Cache node_modules and add a step to validate that headless webpack build emits expected artifacts.

Dependencies & Parallelism

Dependency order (high-level): Foundational (T004-T007) -> Capture (T020-T023) -> Queries/UI (T008-T017, T018-T019) -> Export (T024-T025) -> Tests (T026-T028) -> Polish (T029-T030)

Parallel opportunities: Tasks marked with [P] can be executed in parallel. Many UI tasks are independent and can be parallelized.

Summary

- Total tasks: 36
- Tasks per story: US1: ~19, US2: ~6, US3: ~2, Setup/Foundational/Polish: remainder

Additional Implementation Tasks (small follow-ups)

- [ ] T037 Add export job status endpoint and server-side job store integration - src/modules/Spamma.Modules.EmailInbox.Client/Application/Queries/ExportJobStatusQuery.cs + server-side processor

- [ ] T038 Define and implement audit event emission for capture/export/delete (emit AuditEvents.md schema) - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Logging/AuditEventPublisher.cs

- [ ] T039 Enforce capture synchronous timeout in SpammaMessageStore (100ms recommended) and add unit tests to assert SMTP response latency - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/SpammaMessageStore.cs
