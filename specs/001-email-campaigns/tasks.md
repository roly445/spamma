# Implementation Tasks: Email campaigns

Phase 1: Setup

- [x] T001 Initialize feature plan artifacts in specs/ (spec.md, plan.md, research.md, data-model.md, tasks.md)
- [x] T002 [P] Ensure CI can build Blazor client headless (update pipeline if needed) - .github/workflows/
- [x] T003 Add configuration keys for CampaignCaptureConfig to shared settings - src/modules/Spamma.Modules.Common/Settings.cs

Phase 2: Foundational (blocking prerequisites)

- [x] T004 [P] [US1] Add `CampaignSummary` read model document and Marten projection skeleton - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/CampaignSummaryProjection.cs
- [x] T005 [P] [US1] Add `CampaignSample` document and local message store provider interface implementation stub - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/LocalMessageStoreProvider.cs
- [x] T006 [P] Add configuration for header name `x-spamma-camp` and max header length - src/modules/Spamma.Modules.Common/Settings/CampaignCaptureConfig.cs
- [x] T007 [P] [US1] Add CQRS command DTO `RecordCampaignCaptureCommand` in EmailInbox.Client - src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/RecordCampaignCaptureCommand.cs

Phase 3: User Story 1 - View campaign list (Priority: P1)

- [x] T008 [US1] Implement query `GetCampaignsQuery` DTO in DomainManagement.Client - src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetCampaignsQuery.cs
- [x] T009 [US1] Implement query processor `GetCampaignsQueryProcessor` to read `CampaignSummary` projections - src/modules/Spamma.Modules.DomainManagement/Application/QueryProcessors/GetCampaignsQueryProcessor.cs
- [x] T010 [US1] Add Campaigns page UI skeleton (razor + code-behind) and list component - src/Spamma.App/Spamma.App.Client/Pages/Campaigns.razor, Campaigns.razor.cs
- [x] T011 [US1] Add paging, sorting, and filtering UI and wire to `IQuerier` calls - src/Spamma.App/Spamma.App.Client/Pages/Campaigns.razor.cs
- [x] T012 [US1] Add manual "Refresh" control and wire to re-query backend - src/Spamma.App/Spamma.App.Client/Components/Campaigns/RefreshButton.razor

Phase 4: User Story 2 - Campaign drill-in & sample (Priority: P1)

- [x] T013 [US2] Implement query `GetCampaignDetailQuery` DTO - src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetCampaignDetailQuery.cs
- [x] T014 [US2] Implement query processor `GetCampaignDetailQueryProcessor` returning time-series buckets and sample - src/modules/Spamma.Modules.DomainManagement/Application/QueryProcessors/GetCampaignDetailQueryProcessor.cs
- [x] T015 [US2] Add Campaign detail UI with ApexCharts brush timeline and sample display - src/Spamma.App/Spamma.App.Client/Pages/CampaignDetail.razor, CampaignDetail.razor.cs
- [x] T016 [US2] Integrate ApexCharts via JS interop wrapper and Tailwind-friendly container - src/Spamma.App/Spamma.App.Client/Assets/Scripts/apexcharts-wrapper.ts and wwwroot assets
- [x] T017 [US2] Add UI CTA to open sample message in inbox viewer and wire navigation - src/Spamma.App/Spamma.App.Client/Components/Campaigns/SamplePreview.razor

Phase 5: User Story 3 - Inbox tagging & journey (Priority: P2)

- [x] T018 [US3] Update inbox message list rendering to include campaign badge when header present - src/Spamma.App/Spamma.App.Client/Components/Inbox/MessageRow.razor
- [x] T019 [US3] Add CTA in message viewer linking to campaign detail - src/Spamma.App/Spamma.App.Client/Components/Inbox/MessageViewer.razor

Phase 6: Capture & Projection Implementation (behind User Story flows)

- [x] T020 [US1] Update SMTP ingestion to detect `x-spamma-camp` header, normalize value to lower-case, and publish `RecordCampaignCaptureCommand` - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/SpammaMessageStore.cs
- [x] T021 [US1] Implement `RecordCampaignCaptureCommandHandler` to update `CampaignSummary` projection and create `CampaignSample` when first-seen - src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/RecordCampaignCaptureCommandHandler.cs
- [x] T022 [US1] Ensure receipt/audit event is logged synchronously at ingress (lightweight) - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Logging/SmtpAuditLogger.cs
- [x] T023 [US1] Add retry/backoff for async persistence jobs (use CAP or background workers) - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Workers/CaptureRetryWorker.cs

Phase 7: Export & Admin

- [x] T024 [US1] Implement export endpoint/command using BluQube command with CSV/JSON formats and synchronous limit enforcement - src/modules/Spamma.Modules.DomainManagement/Application/CommandHandlers/ExportCampaignsCommandHandler.cs
- [x] T025 [US1] Audit and rate-limit exports (log requests, add throttling) - src/modules/Spamma.Modules.DomainManagement/Infrastructure/ExportAuditService.cs

Phase 8: Tests & CI

- [x] T026 [US1] Add unit tests for header normalization and validation - tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/SpammaMessageStoreTests.cs
- [x] T027 [US1] Add integration test for end-to-end SMTP -> capture -> projection -> query - tests/Spamma.Modules.EmailInbox.Tests/Integration/SmtpCaptureIntegrationTests.cs
- [x] T028 [US1] Add UI test (minimal) to ensure Campaigns page renders and manual refresh triggers backend call (if UI test infra exists) - tests/Spamma.App.Tests/Client/CampaignsPageTests.cs

Polish & Cross-cutting

- [x] T029 Finalize observability: metrics for captures/sec, projection-lag, export counts - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Observability/CampaignMetrics.cs
- [x] T030 Documentation: update quickstart.md and README with campaign feature instructions - specs/001-email-campaigns/quickstart.md

Additional Critical Tasks (remediation additions)

- [x] T031 [P] Auto-delete exclusion: Implement auto-delete exclusion for campaign-tagged messages.
- [x] T032 [P] Delete Campaign API + `.Client` DTOs and server handler.
- [x] T033 [P] Export Campaign Data `.Client` DTO + Server Export Handler with RBAC and async fallback.
- [x] T034 RBAC enforcement for campaign viewing and exports.
- [x] T035 Frontend: Add ApexCharts dependency and chart wrapper.
- [x] T036 CI: Ensure headless frontend build and apexcharts are built in CI.

Additional Implementation Tasks (small follow-ups)

- [x] T037 Add export job status endpoint and server-side job store integration
- [x] T038 Define and implement audit event emission for capture/export/delete (emit AuditEvents.md schema)
- [x] T039 Enforce capture synchronous timeout in SpammaMessageStore (100ms recommended) and add unit tests to assert SMTP response latency
