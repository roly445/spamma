# Tasks: Developer First Push API

**Input**: Design documents from `/specs/001-email-push-api/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are included as they support verification-based testing patterns per project constitution.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Modular monolith**: `src/modules/Spamma.Modules.EmailInbox/` for server logic, `src/modules/Spamma.Modules.EmailInbox.Client/` for contracts
- **Shared infrastructure**: `src/Spamma.App/Spamma.App/` for app-level setup

## Constitution-Driven Task Categories

- **Code Quality**
  - [ ] CQ-001 Run solution build and fix compiler errors/warnings (dotnet build).
  - [ ] CQ-002 Run static analyzers (StyleCop/Sonar) and address any violations.
  - [ ] CQ-003 Ensure one public type per file and remove any XML doc comments used for primary intent; prefer clear naming and small focused types.

- **Compiler Warnings**
  - [ ] CW-001 Treat compiler warnings as failures in CI for the feature branch.
  - [ ] CW-002 Fix or suppress library-specific unavoidable warnings with commented rationale.

- **Blazor Component Split**
  - [ ] BZ-001 Convert interactive `.razor` components to `.razor` + `.razor.cs` code-behind.
  - [ ] BZ-002 Ensure server-rendered static pages are marked with `[ExcludeFromInteractiveRouting]` where applicable.

- **Commands & Queries Placement**
  - [ ] CQP-001 Add command/query DTOs to the corresponding `.Client` project under `Application/Commands` or `Application/Queries` with `[BluQubeCommand|BluQubeQuery]` attributes where required.
  - [ ] CQP-002 Implement handlers, validators and authorizers in the non-`.Client` server project in `Application/CommandHandlers` or `Application/QueryProcessors`.

- **Project Additions Approval**
  - [ ] PA-001 If a new csproj is needed, add a PR justification and get explicit maintainer approval before creating the project.

- **Tests**
  - [ ] TST-001 Add unit tests for domain logic (happy path and key failure modes).
  - [ ] TST-002 Add at least one integration test that exercises the full ingestion or API flow when applicable.

- **Observability**
  - [ ] OBS-001 Add structured logging (Serilog/Microsoft.Extensions.Logging) for new flows.
  - [ ] OBS-002 Add metrics/tracing hooks where long-running or async operations exist.

- **Security & Privacy**
  - [ ] SEC-001 Ensure no secrets are committed; reference secrets via environment variables or user-secrets.
  - [ ] SEC-002 Add authorization checks and unit tests for permission boundaries.

## Dependencies

**User Story Completion Order**:

- US1 (Set up Email Push Integration) → US2 (Receive Email Push Notifications) → US3 (Fetch Full Email Content)
- US1 and US2 are both P1 priority and can be developed in parallel after foundational setup
- US3 depends on US2 for email notification flow

**Parallel Execution Opportunities**:

- Within US1: Model creation, command setup, validation can run in parallel
- Within US2: gRPC service implementation, projection setup, email filtering logic can run in parallel
- Within US3: Email content retrieval endpoint, permission checks can run in parallel

## Implementation Strategy

**MVP Scope**: US1 + US2 (complete push integration setup and notification delivery)
**Incremental Delivery**: Each user story delivers independently testable value
**Testing Approach**: Verification-based unit tests for domain logic, integration tests for full flows

## Phase 1: Setup (Project Structure)

**Purpose**: Initialize project structure and dependencies for push API feature

- [X] T001 Add gRPC.AspNetCore package to Spamma.App.csproj
- [X] T002 Create contracts directory structure in specs/001-email-push-api/contracts/
- [X] T003 Generate C# gRPC client from email_push.proto in Spamma.Modules.EmailInbox.Client

## Phase 2: Foundational (Shared Infrastructure)

**Purpose**: Set up shared infrastructure required by all user stories

- [X] T004 [P] Create PushIntegration aggregate in src/modules/Spamma.Modules.EmailInbox/Domain/PushIntegrationAggregate/
- [X] T005 [P] Create PushIntegration events in src/modules/Spamma.Modules.EmailInbox/Domain/PushIntegrationAggregate/Events/
- [X] T006 [P] Create PushIntegrationRepository in src/modules/Spamma.Modules.EmailInbox/Infrastructure/Repositories/
- [X] T007 [P] Create PushIntegrationProjection in src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/
- [X] T008 [P] Register Marten projection in EmailInboxModule.cs
- [X] T009 [P] Create EmailPushService gRPC service in src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/
- [X] T010 [P] Register gRPC service in Spamma.App/Program.cs
- [X] T011 [P] Add JWT validation utility in src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/

## Phase 3: US1 - Set up Email Push Integration

**Story Goal**: Allow developers to create and manage push integrations for email notifications

**Independent Test Criteria**: Can create, update, delete integrations via API without email flow

**Implementation Tasks**:

- [X] T012 [P] [US1] Create CreatePushIntegrationCommand in src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/
- [X] T013 [P] [US1] Create UpdatePushIntegrationCommand in src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/
- [X] T014 [P] [US1] Create DeletePushIntegrationCommand in src/modules/Spamma.Modules.EmailInbox.Client/Application/Commands/
- [X] T015 [P] [US1] Create GetPushIntegrationsQuery in src/modules/Spamma.Modules.EmailInbox.Client/Application/Queries/
- [X] T016 [P] [US1] Implement CreatePushIntegrationCommandHandler in src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/
- [X] T017 [P] [US1] Implement UpdatePushIntegrationCommandHandler in src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/
- [X] T018 [P] [US1] Implement DeletePushIntegrationCommandHandler in src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/
- [X] T019 [P] [US1] Implement GetPushIntegrationsQueryProcessor in src/modules/Spamma.Modules.EmailInbox/Application/QueryProcessors/
- [X] T020 [P] [US1] Create command validators in src/modules/Spamma.Modules.EmailInbox/Application/Validators/
- [X] T021 [P] [US1] Create authorizers in src/modules/Spamma.Modules.EmailInbox/Application/Authorizers/
- [X] T022 [P] [US1] REST API auto-generated via BluQube attributes on commands/queries (no manual controller needed)

## Phase 4: US2 - Receive Email Push Notifications

**Story Goal**: Deliver real-time email notifications via gRPC streaming

**Independent Test Criteria**: Can simulate email arrival and verify notification delivery to connected clients

**Implementation Tasks**:

- [X] T023 [P] [US2] Implement JWT authentication in EmailPushService
- [X] T024 [P] [US2] Implement gRPC SubscribeToEmails streaming method
- [X] T025 [P] [US2] Create email filtering logic for integrations
- [X] T026 [P] [US2] Integrate with existing email ingestion flow to trigger notifications
- [X] T027 [P] [US2] Add connection management and error handling in gRPC service
- [X] T028 [P] [US2] Create EmailNotification DTO in src/modules/Spamma.Modules.EmailInbox.Client/Application/DTOs/

## Phase 5: US3 - Fetch Full Email Content

**Story Goal**: Allow retrieval of complete email content via REST API

**Independent Test Criteria**: Can request email content by ID and receive EML file

**Implementation Tasks**:

- [X] T029 [US3] Create GetEmailContentQuery in src/modules/Spamma.Modules.EmailInbox.Client/Application/Queries/
- [X] T030 [US3] Implement GetEmailContentQueryProcessor in src/modules/Spamma.Modules.EmailInbox/Application/QueryProcessors/
- [X] T031 [US3] Create email content retrieval endpoint in src/modules/Spamma.Modules.EmailInbox/Infrastructure/Api/
- [X] T032 [US3] Add permission checks for email content access
- [X] T033 [US3] Implement EML file generation and streaming

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final integration, testing, and quality assurance

- [X] T034 [P] Add integration tests for full push notification flow
- [X] T035 [P] Add unit tests for domain logic and validation
- [X] T036 [P] Update quickstart documentation with working examples
- [X] T037 [P] Add observability logging for push operations
- [X] T038 [P] Performance testing and optimization for concurrent connections
- [X] T039 [P] Security audit and penetration testing for gRPC endpoints
- [X] T040 Final integration testing across all user stories