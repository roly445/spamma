# Tasks: API Key Management

**Input**: Design documents from `/specs/001-api-key-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included as required by constitution (unit tests for domain logic, integration tests for API flow).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Progress**: ✅ Phase 1 (Setup) & Phase 2 (Foundational) completed. Ready for Phase 3 (User Story implementation).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Task Categories (Constitution-driven)

The project's constitution requires explicit task categories to be included in every generated `tasks.md`.

- Code Quality
  - [ ] CQ-001 Run solution build and fix compiler errors/warnings (dotnet build).
  - [ ] CQ-002 Run static analyzers (StyleCop/Sonar) and address any violations.
  - [ ] CQ-003 Ensure one public type per file and remove any XML doc comments used for primary intent; prefer clear naming and small focused types.

- Compiler Warnings
  - [ ] CW-001 Treat compiler warnings as failures in CI for the feature branch.
  - [ ] CW-002 Fix or suppress library-specific unavoidable warnings with commented rationale.

- Blazor Component Split
  - [ ] BZ-001 Convert interactive `.razor` components to `.razor` + `.razor.cs` code-behind.
  - [ ] BZ-002 Ensure server-rendered static pages are marked with `[ExcludeFromInteractiveRouting]` where applicable.

- Commands & Queries Placement
  - [ ] CQP-001 Add command/query DTOs to the corresponding `.Client` project under `Application/Commands` or `Application/Queries` with `[BluQubeCommand|BluQubeQuery]` attributes where required.
  - [ ] CQP-002 Implement handlers, validators and authorizers in the non-`.Client` server project in `Application/CommandHandlers` or `Application/QueryProcessors`.

- Project Additions Approval
  - [ ] PA-001 If a new csproj is needed, add a PR justification and get explicit maintainer approval before creating the project.

- Tests
  - [ ] TST-001 Add unit tests for domain logic (happy path and key failure modes).
  - [ ] TST-002 Add at least one integration test that exercises the full ingestion or API flow when applicable.

- Observability
  - [ ] OBS-001 Add structured logging (Serilog/Microsoft.Extensions.Logging) for new flows.
  - [ ] OBS-002 Add metrics/tracing hooks where long-running or async operations exist.

- Security & Privacy
  - [ ] SEC-001 Ensure no secrets are committed; reference secrets via environment variables or user-secrets.
  - [ ] SEC-002 Add authorization checks and unit tests for permission boundaries.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create ApiKeys directory structure in Spamma.Modules.UserManagement.Client/Application/Commands/ApiKeys/
- [x] T002 Create ApiKeys directory structure in Spamma.Modules.UserManagement.Client/Application/Queries/ApiKeys/
- [x] T003 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Domain/ApiKeys/
- [x] T004 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Application/CommandHandlers/ApiKeys/
- [x] T005 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Application/QueryProcessors/ApiKeys/
- [x] T006 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Infrastructure/Repositories/
- [x] T007 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Infrastructure/Services/
- [x] T008 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Infrastructure/Projections/
- [x] T009 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Tests/Domain/
- [x] T010 Create ApiKeys directory structure in Spamma.Modules.UserManagement/Tests/Application/
- [x] T011 Create ApiKeys directory structure in Spamma.App/Spamma.App.Client/Components/ApiKeys/
- [x] T012 Create ApiKeys directory structure in Spamma.App/Spamma.App/Infrastructure/Services/

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T013 Create database migration for API keys table in Spamma.Modules.UserManagement/Infrastructure/Migrations/
- [x] T014 [P] Implement IApiKeyRepository interface in Spamma.Modules.UserManagement/Infrastructure/Repositories/IApiKeyRepository.cs
- [x] T015 [P] Implement ApiKeyValidationService in Spamma.Modules.UserManagement/Infrastructure/Services/ApiKeyValidationService.cs
- [x] T016 [P] Implement ApiKeyAuthenticationHandler in Spamma.Modules.UserManagement/Infrastructure/Services/ApiKeyAuthenticationHandler.cs
- [x] T017 Configure API key authentication middleware in Spamma.App/Spamma.App/Program.cs
- [x] T018 Register API key services in Spamma.Modules.UserManagement/Module.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

## Phase 3: User Story 1 - Create API Keys (Priority: P1) 🎯 MVP

**Goal**: Enable authenticated users to create API keys with custom names

**Independent Test**: User can create an API key through UI, see it displayed once, and find it in their key list

### Tests for User Story 1 ⚠️

- [ ] T019 [P] [US1] Unit tests for API key creation domain logic in Spamma.Modules.UserManagement.Tests/Domain/ApiKeys/ApiKeyTests.cs
- [ ] T020 [P] [US1] Unit tests for CreateApiKeyCommandHandler in Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/ApiKeys/CreateApiKeyCommandHandlerTests.cs
- [ ] T021 [US1] Integration test for full API key creation flow in Spamma.Modules.UserManagement.Tests/ApiKeyLifecycleTests.cs

### Implementation for User Story 1

- [x] T022 [P] [US1] Create ApiKey entity in Spamma.Modules.UserManagement/Domain/ApiKeys/ApiKey.cs
- [x] T023 [P] [US1] Create ApiKeyCreated event in Spamma.Modules.UserManagement/Domain/ApiKeys/Events/ApiKeyCreated.cs
- [x] T024 [P] [US1] Create CreateApiKeyCommand in Spamma.Modules.UserManagement.Client/Application/Commands/ApiKeys/CreateApiKeyCommand.cs
- [x] T025 [P] [US1] Create CreateApiKeyCommandResult in Spamma.Modules.UserManagement.Client/Application/Commands/ApiKeys/CreateApiKeyCommandResult.cs
- [x] T026 [US1] Implement CreateApiKeyCommandHandler in Spamma.Modules.UserManagement/Application/CommandHandlers/ApiKeys/CreateApiKeyCommandHandler.cs
- [x] T027 [US1] Implement CreateApiKeyCommandValidator in Spamma.Modules.UserManagement/Application/Validators/CreateApiKeyCommandValidator.cs
- [x] T028 [US1] Implement CreateApiKeyCommandAuthorizer in Spamma.Modules.UserManagement/Application/Authorizers/Commands/CreateApiKeyCommandAuthorizer.cs
- [x] T029 [US1] Create ApiKeyProjection in Spamma.Modules.UserManagement/Infrastructure/Projections/ApiKeyProjection.cs
- [x] T030 [US1] Implement ApiKeyRepository in Spamma.Modules.UserManagement/Infrastructure/Repositories/ApiKeyRepository.cs
- [x] T031 [US1] Create ApiKeyManager Blazor component in Spamma.App/Spamma.App.Client/Components/ApiKeys/ApiKeyManager.razor
- [x] T032 [US1] Create ApiKeyManager code-behind in Spamma.App/Spamma.App.Client/Components/ApiKeys/ApiKeyManager.razor.cs

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

## Phase 4: User Story 2 - Revoke API Keys (Priority: P1)

**Goal**: Enable authenticated users to revoke API keys via UI

**Independent Test**: User can revoke an existing API key and verify it no longer works for authentication

### Tests for User Story 2 ⚠️

- [ ] T033 [P] [US2] Unit tests for API key revocation domain logic in Spamma.Modules.UserManagement.Tests/Domain/ApiKeys/ApiKeyRevocationTests.cs
- [ ] T034 [P] [US2] Unit tests for RevokeApiKeyCommandHandler in Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/ApiKeys/RevokeApiKeyCommandHandlerTests.cs
- [ ] T035 [US2] Integration test for API key revocation flow in Spamma.Modules.UserManagement.Tests/ApiKeyRevocationTests.cs

### Implementation for User Story 2

- [ ] T036 [P] [US2] Create ApiKeyRevoked event in Spamma.Modules.UserManagement/Domain/ApiKeys/Events/ApiKeyRevoked.cs
- [ ] T037 [P] [US2] Create RevokeApiKeyCommand in Spamma.Modules.UserManagement.Client/Application/Commands/ApiKeys/RevokeApiKeyCommand.cs
- [ ] T038 [US2] Implement RevokeApiKeyCommandHandler in Spamma.Modules.UserManagement/Application/CommandHandlers/ApiKeys/RevokeApiKeyCommandHandler.cs
- [ ] T039 [US2] Implement RevokeApiKeyCommandValidator in Spamma.Modules.UserManagement/Application/Validators/RevokeApiKeyCommandValidator.cs
- [ ] T040 [US2] Implement RevokeApiKeyCommandAuthorizer in Spamma.Modules.UserManagement/Application/Authorizers/Commands/RevokeApiKeyCommandAuthorizer.cs
- [ ] T041 [US2] Update ApiKeyProjection to handle revocation in Spamma.Modules.UserManagement/Infrastructure/Projections/ApiKeyProjection.cs
- [ ] T042 [US2] Update ApiKeyManager component to include revoke functionality in Spamma.App/Spamma.App.Client/Components/ApiKeys/ApiKeyManager.razor

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

## Phase 5: User Story 3 - View API Keys (Priority: P2)

**Goal**: Enable authenticated users to view their API keys and status

**Independent Test**: User can see list of their API keys with metadata but not the actual key values

### Tests for User Story 3 ⚠️

- [ ] T043 [P] [US3] Unit tests for GetApiKeysQueryProcessor in Spamma.Modules.UserManagement.Tests/Application/QueryProcessors/ApiKeys/GetApiKeysQueryProcessorTests.cs
- [ ] T044 [US3] Integration test for API key listing in Spamma.Modules.UserManagement.Tests/ApiKeyListingTests.cs

### Implementation for User Story 3

- [ ] T045 [P] [US3] Create GetApiKeysQuery in Spamma.Modules.UserManagement.Client/Application/Queries/ApiKeys/GetApiKeysQuery.cs
- [ ] T046 [P] [US3] Create GetApiKeysQueryResult in Spamma.Modules.UserManagement.Client/Application/Queries/ApiKeys/GetApiKeysQueryResult.cs
- [ ] T047 [US3] Implement GetApiKeysQueryProcessor in Spamma.Modules.UserManagement/Application/QueryProcessors/ApiKeys/GetApiKeysQueryProcessor.cs
- [ ] T048 [US3] Implement GetApiKeysQueryAuthorizer in Spamma.Modules.UserManagement/Application/Authorizers/Queries/GetApiKeysQueryAuthorizer.cs
- [ ] T049 [US3] Create ApiKeyList Blazor component in Spamma.App/Spamma.App.Client/Components/ApiKeys/ApiKeyList.razor
- [ ] T050 [US3] Create ApiKeyList code-behind in Spamma.App/Spamma.App.Client/Components/ApiKeys/ApiKeyList.razor.cs

**Checkpoint**: All core user stories should now be independently functional

## Phase 6: User Story 4 - API Key Authentication (Priority: P1)

**Goal**: Replace JWT authentication with API key authentication for public endpoints

**Independent Test**: API requests to public endpoints work with API keys but fail with invalid/revoked keys

### Tests for User Story 4 ⚠️

- [ ] T051 [P] [US4] Unit tests for ApiKeyAuthenticationHandler in Spamma.Modules.UserManagement.Tests/Infrastructure/Services/ApiKeyAuthenticationHandlerTests.cs
- [ ] T052 [US4] Integration test for API key authentication on public endpoints in Spamma.App.Tests/ApiKeyAuthenticationTests.cs

### Implementation for User Story 4

- [ ] T053 [US4] Update authentication configuration to include API key handler in Spamma.App/Spamma.App/Program.cs
- [ ] T054 [US4] Add API key authentication to public endpoint authorization policies in Spamma.App/Spamma.App/Program.cs
- [ ] T055 [US4] Update existing public endpoints to support dual authentication during transition in relevant controllers
- [ ] T056 [US4] Add caching for API key validation in ApiKeyValidationService.cs
- [ ] T057 [US4] Add structured logging for authentication attempts in ApiKeyAuthenticationHandler.cs

**Checkpoint**: API key authentication fully functional for all public endpoints

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T058 [P] Documentation updates in specs/001-api-key-management/
- [ ] T059 Code cleanup and refactoring across all API key components
- [ ] T060 Performance optimization for API key validation under load
- [ ] T061 [P] Additional unit tests for edge cases in Spamma.Modules.UserManagement.Tests/
- [ ] T062 Security hardening and audit logging improvements
- [ ] T063 Run quickstart.md validation and update if needed
- [ ] T064 Remove JWT authentication code after transition period
- [ ] T065 Update API documentation with API key authentication examples

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Depends on US1 for basic API key infrastructure
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 4 (P1)**: Can start after Foundational (Phase 2) - Depends on US1 for API key creation

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Domain entities before commands/queries
- Commands/queries before handlers/processors
- Infrastructure before UI components
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, User Stories 1, 3, and 4 can start in parallel
- User Story 2 depends on User Story 1 completion
- All tests for a user story marked [P] can run in parallel
- Different components within a story can be parallelized where marked [P]

## Parallel Example: User Story 1

```bash
# Launch domain and command definition tasks together:
Task: "Create ApiKey entity in Spamma.Modules.UserManagement/Domain/ApiKeys/ApiKey.cs"
Task: "Create ApiKeyCreated event in Spamma.Modules.UserManagement/Domain/ApiKeys/Events/ApiKeyCreated.cs"
Task: "Create CreateApiKeyCommand in Spamma.Modules.UserManagement.Client/Application/Commands/ApiKeys/CreateApiKeyCommand.cs"
Task: "Create CreateApiKeyCommandResult in Spamma.Modules.UserManagement.Client/Application/Commands/ApiKeys/CreateApiKeyCommandResult.cs"

# Launch test tasks together:
Task: "Unit tests for API key creation domain logic in Spamma.Modules.UserManagement.Tests/Domain/ApiKeys/ApiKeyTests.cs"
Task: "Unit tests for CreateApiKeyCommandHandler in Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/ApiKeys/CreateApiKeyCommandHandlerTests.cs"
```

## Implementation Strategy

### MVP First (User Stories 1 + 4 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (API key creation)
4. Complete Phase 6: User Story 4 (API key authentication)
5. **STOP and VALIDATE**: Test API key creation and authentication independently
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 4 → Test authentication → Deploy/Demo
4. Add User Story 2 → Test revocation → Deploy/Demo
5. Add User Story 3 → Test viewing → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Stories 1 & 4 (core authentication)
   - Developer B: User Story 2 (revocation)
   - Developer C: User Story 3 (viewing) + UI polish
3. Stories complete and integrate independently

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence