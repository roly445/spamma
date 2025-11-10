---

description: "Task list for feature: Bug sweep — minor bugs & niggles"

---

# Tasks: Bug sweep — minor bugs & niggles

**Input**: Design documents from `/specs/001-bug-sweep/` (plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/)

## Format

Each task follows the required checklist format. Tasks are grouped by phase and by user story so each story can be implemented and tested independently.

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Ensure branch `001-bug-sweep` is checked out and up-to-date (repo root)
- [X] T002 [P] Add/verify local dev run instructions in `specs/001-bug-sweep/quickstart.md`
- [X] T003 [P] Run `dotnet build` and fix any compiler warnings or errors introduced by existing branches (`Spamma.sln`)
- [X] T004 [P] Verify test projects can run: `dotnet test tests/Spamma.Modules.EmailInbox.Tests/` and `dotnet test tests/Spamma.App.Tests/` (repo root)

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T005 Configure and confirm the EmailInbox error codes file exists: `src/modules/Spamma.Modules.EmailInbox.Client/Contracts/EmailInboxErrorCodes.cs`
- [X] T006 [P] Add new error code constant `EmailIsPartOfCampaign` to `src/modules/Spamma.Modules.EmailInbox.Client/Contracts/EmailInboxErrorCodes.cs` (add line near other constants)
- [X] T007 [P] Add unit test project references (if missing) so `tests/Spamma.Modules.EmailInbox.Tests/` can reference `Spamma.Modules.EmailInbox` server project
- [X] T008 [P] Ensure test helpers and verification extensions are available in `tests/Spamma.Tests.Common/Verification/` (used by handler tests)
- [X] T009 Ensure CI-like build locally: `dotnet build` then `dotnet test --no-build` passes for current baseline (repo root)

**Checkpoint**: Foundational tasks complete → user story implementation may begin

---

## Phase 3: User Story 1 - Fix blocking UI & campaign-protection (Priority: P1) 🎯 MVP

**Goal**: Protect campaign-bound emails from deletion and favouriting; hide Delete/Favorite controls for campaign-bound emails in the client UI. Deliver a server-authoritative protection and a matching client UX update.

**Independent Test**: Unit tests for handlers (protected & unprotected cases) and an API contract test asserting `BluQubeErrorData` contains `EmailIsPartOfCampaign`. Manual UI smoke test: in the inbox, campaign-bound emails show no Delete/Favorite controls.

### Tests (required per FR-007)

- [X] T010 [P] [US1] Add unit test: `tests/Spamma.Modules.EmailInbox.Tests/Application/CommandHandlers/Email/DeleteEmailCommandHandlerTests.cs` — test that deleting an email with `CampaignId != null` returns a failed `CommandResult` with `BluQubeErrorData` and code `EmailIsPartOfCampaign`.
- [X] T011 [P] [US1] Add unit test: `tests/Spamma.Modules.EmailInbox.Tests/Application/CommandHandlers/Email/ToggleEmailFavoriteCommandHandlerTests.cs` — test that toggling favorite on an email with `CampaignId != null` returns failed `CommandResult` with `BluQubeErrorData` and code `EmailIsPartOfCampaign`.
- [ ] T012 [P] [US1] Add API contract/integration test: `tests/Spamma.Modules.EmailInbox.Tests/Integration/Contract/EmailCampaignProtectionTests.cs` — call the Delete and Toggle endpoints and assert response contains BluQubeErrorData with `EmailIsPartOfCampaign` (do not assert specific HTTP status code).

### Implementation

- [X] T013 [US1] Edit handler: `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/DeleteEmailCommandHandler.cs` — before performing `email.Delete(...)`, if `email` has a non-null `CampaignId`, return `CommandResult.Failed(new BluQubeErrorData(EmailInboxErrorCodes.EmailIsPartOfCampaign, "Email is part of a campaign and cannot be deleted."))`.
- [X] T014 [US1] Edit handler: `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/ToggleEmailFavoriteCommandHandler.cs` — before marking/unmarking favorite, if `email.CampaignId != null` return `CommandResult.Failed(new BluQubeErrorData(EmailInboxErrorCodes.EmailIsPartOfCampaign, "Email is part of a campaign and cannot be favorited or unfavorited."))`.
- [X] T015 [US1] Add or update error code reference: `src/modules/Spamma.Modules.EmailInbox.Client/Contracts/EmailInboxErrorCodes.cs` — ensure `public const string EmailIsPartOfCampaign = "email_inbox.email_part_of_campaign";` is present.
- [ ] T016 [US1] Update server-side logs where handler returns failure: ensure structured logging in `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/*` follows pattern used elsewhere (optional OBS-001).

### Client UI

- [ ] T017 [US1] Edit client component: `src/Spamma.App/Spamma.App.Client/Components/UserControls/EmailViewer.razor` (markup) — hide Delete button and Favorite toggle if `Email.CampaignId != null` (or add `@if (Email.CampaignId == null) { ... }`).
- [ ] T018 [US1] Edit client code-behind if present: `src/Spamma.App/Spamma.App.Client/Components/UserControls/EmailViewer.razor.cs` — ensure methods calling `commander.Send(new DeleteEmailCommand(...))` and `ToggleEmailFavoriteCommand` are guarded/disabled or that UI prevents invocation.
- [ ] T019 [US1] Add/adjust component tests: `tests/Spamma.App.Tests/Components/EmailViewerTests.cs` — assert Delete/Favorite buttons are hidden for an `Email` model with `CampaignId` set.

**Checkpoint**: Server enforces protection; client hides controls; unit and contract tests added and passing

---

## Phase 4: User Story 2 - Fix backend error surfaces (Priority: P2)

**Goal**: Ensure errors surfaced to users are user-friendly and do not leak stack traces; ensure server returns BluQube error shapes.

**Independent Test**: Trigger a recoverable failure and assert API returns BluQubeErrorData (tests in integration project).

### Implementation

- [ ] T020 [US2] Locate server endpoints that can surface raw exceptions; add consistent BluQube error mapping in `src/Spamma.App/Spamma.App/` middleware or existing error mapping code (file path: `src/Spamma.App/Spamma.App/Startup` or similar).
- [ ] T021 [US2] Add integration test: `tests/Spamma.App.Tests/Integration/ErrorResponsesTests.cs` — provoke a known validation error and assert BluQubeErrorData is present and user-friendly.
- [ ] T022 [US2] Update UI error handlers where raw messages are surfaced (search for direct exception outputs in `src/Spamma.App/` components) and change to show friendly strings.

---

## Phase 5: User Story 3 - Data/projection inconsistencies (Priority: P3)

**Goal**: Fix small data/projection mismatches (counts, timestamps)

**Independent Test**: Create sample data and run projections; assert read-model values match expectations.

### Implementation

- [ ] T023 [US3] Add unit/integration test: `tests/Spamma.Modules.EmailInbox.Tests/Projections/EmailLookupProjectionTests.cs` — ensure projection maps CampaignId and timestamps correctly.
- [ ] T024 [US3] Edit projection: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/EmailLookupProjection.cs` — ensure `CampaignId` is included and timestamp mapping is correct (if missing).
- [ ] T025 [US3] Update read-model: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/ReadModels/EmailLookup.cs` — validate properties and types (CampaignId: Guid?).

---

## Phase N: Polish & Cross-Cutting Concerns

- [ ] T026 [P] Documentation: update `specs/001-bug-sweep/quickstart.md` with final run steps and test commands
- [ ] T027 [P] CQ-001: Run `dotnet build` and fix any new compiler warnings introduced by these changes
- [ ] T028 [P] TST-001: Add additional unit test coverage for edge cases (deleted + campaign-bound simultaneously)
- [ ] T029 [P] OBS-001: Add structured logging for handler failures where appropriate
- [ ] T030 [P] SEC-001: Ensure no secrets in commits and that new code follows existing auth patterns

- [ ] T031 [P] QA-001: Automated JS console checks — implement a headless browser smoke test (Playwright or equivalent) that loads Login, Setup, Inbox pages and asserts zero uncaught console errors for defined scenarios.
- [ ] T032 [P] INF-001: Locate error-mapping middleware — confirm the exact file(s) that perform BluQube error mapping in `src/Spamma.App/` and update T020 with the concrete path(s).
- [ ] T033 [P] INF-002: Verify test project paths — run a quick script or manual check to confirm the test file paths referenced in tasks (integration and unit test paths) match the repository layout; update tasks if differences are found.

---

## Dependencies & Execution Order

- Foundational tasks (T005-T009) must complete before User Story phases begin
- User Story phases (US1..US3) can be worked in parallel after foundation completes
- Tests (T010-T012 etc.) should be added early in each story and run as part of CI

## Parallel opportunities identified

- T002, T003, T006, T007, T008 can run in parallel
- Unit tests for Delete and Toggle handlers (T010, T011) are parallelizable with implementation once test scaffolding is present
- Client UI tasks (T017-T019) can be implemented in parallel with server handler work as long as server code is merged or mocked in tests

---

## Summary: counts

- Total tasks generated: 30
- Tasks per story:
  - US1 (P1): 11 tasks (T010-T019 + T013-T016) grouped across tests, handlers, and UI
  - US2 (P2): 3 tasks (T020-T022)
  - US3 (P3): 3 tasks (T023-T025)
  - Setup & Foundational: 9 tasks (T001-T009)
  - Polish & Cross-cutting: 5 tasks (T026-T030)


---
