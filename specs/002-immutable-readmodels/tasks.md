# Implementation Tasks: Immutable ReadModels

**Feature**: `002-immutable-readmodels` | **Branch**: `002-immutable-readmodels` | **Date**: November 17, 2025

**Specification**: [Immutable ReadModels Spec](spec.md) | **Plan**: [Implementation Plan](plan.md)

---

## Overview

This document contains the detailed task breakdown for implementing immutable readmodels across the Spamma modular monolith. The feature involves 4 user stories organized by priority (P1 = blocking, P2 = important), with 9 readmodel classes to modify across 3 modules.

**Total Tasks**: 31 tasks across 6 phases

**Task Organization**:
- **Phase 1**: Setup & Validation Spike (FR-009)
- **Phase 2**: Foundational Work (readmodel enumeration & documentation)
- **Phase 3**: User Story 1 - Enforce Immutability at Compile Time (P1)
- **Phase 4**: User Story 2 - Enable Marten Patching (P1)
- **Phase 5**: User Story 3 - Standardize Construction (P2)
- **Phase 6**: User Story 4 - Maintain Backward Compatibility (P2)
- **Phase 7**: Polish & Cross-Cutting Concerns

---

## Parallel Execution Opportunities

**US1 & US2 (Both P1)**: Can execute in parallel after Phase 1 validation spike completes. Each story modifies different readmodels, so no file conflicts.

**US3 & US4 (Both P2)**: Can execute in parallel after US1+US2 complete. Verification tests can run concurrently.

**Suggested MVP Scope**: US1 + US2 (both P1) = core immutability + Marten compatibility, ~4-5 hours

---

## Phase 1: Setup & Validation Spike

**Goal**: Validate that Marten's Patch API works with private setters before bulk changes (FR-009)

**Independent Test Criteria**: Unit test passes; Patch operations successfully update readmodel properties with private setters

### Tasks

- [x] T001 Create validation spike test file `tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/MarteniPatchPrivateSetterValidationTest.cs`
- [x] T002 [P] Create minimal test readmodel with all property patterns: `public Prop { get; private set; }` and `public List<T> Items { get; } = new()`
- [x] T003 [P] Implement test cases:
  - Patch operation on string property with private setter succeeds
  - Patch operation on list property with private setter succeeds
  - Patch operation on nullable datetime with private setter succeeds
- [x] T004 Run validation spike test suite; verify all tests pass
- [x] T005 Document findings in `research.md` Validation Spike section (if test fails, investigate Marten configuration)
- [x] T006 Commit validation spike: `git commit -m "test: Add Marten Patch private setter validation spike"`

**Parallel**: T002 and T003 can run concurrently with T001

---

## Phase 2: Foundational Work

**Goal**: Enumerate all readmodels, document conversion patterns, create quickstart guide for developers

**Independent Test Criteria**: Complete inventory of 9 readmodels with property counts; conversion pattern documented and reviewed; no readmodels missed

### Tasks

- [x] T007 Create detailed data model analysis in `specs/002-immutable-readmodels/data-model.md` listing:
  - All 9 readmodels with module location
  - Property count for each
  - Property types (auto-property, collection, nullable, complex)
  - Current initialization patterns
- [x] T008 [P] Create quickstart guide in `specs/002-immutable-readmodels/quickstart.md` with:
  - Pattern examples: `{ get; private set; }` and `{ get; } = new()`
  - Before/after code snippets for each pattern
  - Projection compatibility notes (object initializers, Patch operations)
  - Common pitfalls and how to avoid them
- [x] T009 [P] Create readmodel conversion checklist in `specs/002-immutable-readmodels/checklists/conversion.md`:
  - Checkbox for each of 9 readmodels
  - Checkbox for each module's projection tests
  - Checkbox for build validation (zero warnings)
  - Checkbox for backward compatibility test
- [x] T010 Review data-model.md and quickstart.md for accuracy and completeness
- [x] T011 Commit foundational documents: `git commit -m "docs: Add data model analysis and quickstart guide for immutable readmodels"`

**Parallel**: T008 and T009 can run concurrently with T007

---

## Phase 3: User Story 1 - Enforce Immutability at Compile Time (P1)

**Goal**: Convert all 9 readmodel classes to use private setters; ensure compiler prevents direct property mutation

**Acceptance Criteria**:
1. All readmodel properties have `{ get; private set; }` or `{ get; } = new()`
2. Compiler prevents direct property assignment to readmodels (verified by attempting assignment and confirming compile error)
3. No public properties have public setters remaining
4. Build passes with zero warnings

**Independent Test Criteria**: 
- Attempted property mutation on readonly instance fails at compile time with clear error message
- Code analysis shows 0 public setters across all 9 readmodels

### Tasks

- [ ] T012 [P] [US1] Convert UserManagement readmodels: `src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/UserLookup.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize list properties: `public List<Guid> ModeratedDomains { get; } = new();`
- [ ] T013 [P] [US1] Convert UserManagement readmodels: `src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/PasskeyProjection.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize any collection properties with `= new()`
- [ ] T014 [P] [US1] Convert UserManagement readmodels: `src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/ApiKeyProjection.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize any collection properties with `= new()`
- [ ] T015 [P] [US1] Convert DomainManagement readmodels: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/DomainLookup.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize list properties: `public List<SubdomainLookup> Subdomains { get; } = new();` etc.
- [ ] T016 [P] [US1] Convert DomainManagement readmodels: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/SubdomainLookup.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize any collection properties with `= new()`
- [ ] T017 [P] [US1] Convert DomainManagement readmodels: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/ChaosAddressLookup.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize any collection properties with `= new()`
- [ ] T018 [P] [US1] Convert EmailInbox readmodels: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/ReadModels/EmailLookup.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize any collection properties with `= new()`
- [ ] T019 [P] [US1] Convert EmailInbox readmodels: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/ReadModels/CampaignSummary.cs`
  - Change all properties to `{ get; private set; }`
  - Initialize any collection properties with `= new()`
- [ ] T020 [US1] Build solution and verify zero compiler warnings: `dotnet build Spamma.sln`
- [ ] T021 [US1] Create compilation failure test: Add test that verifies attempting to directly set a readonly readmodel property fails at compile time
- [ ] T022 [US1] Commit US1 changes: `git commit -m "refactor: Convert all readmodels to immutable properties with private setters"`

**Parallel**: T012-T019 (readmodel conversions) can all run in parallel as they modify different files with no dependencies

**Acceptance Test**: 
```csharp
// This should NOT compile:
var lookup = new UserLookup();
lookup.Name = "new value"; // Compile error: property setter not accessible
```

---

## Phase 4: User Story 2 - Enable Marten Patching with Private Setters (P1)

**Goal**: Verify all 8+ projections work correctly with immutable readmodels using Patch operations

**Acceptance Criteria**:
1. All projection `Create` methods use object initializer syntax (already compatible)
2. All projection `Patch()` operations successfully update private-setter properties
3. All existing projection tests pass without modification
4. Integration tests confirm Patch operations work end-to-end with events

**Independent Test Criteria**:
- Patch operation successfully updates each readmodel property type (string, list, datetime, nullable)
- Event sourcing triggers projection which updates readmodel correctly
- No runtime errors from Patch operations

### Tasks

- [ ] T023 [P] [US2] Verify UserManagement projections: `src/modules/Spamma.Modules.UserManagement/Infrastructure/Projections/UserLookupProjection.cs`
  - Confirm all `Create` methods use object initializers
  - Confirm all `Patch()` operations compatible with private setters
  - Run tests to verify: `dotnet test tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/`
- [ ] T024 [P] [US2] Verify UserManagement projections: `src/modules/Spamma.Modules.UserManagement/Infrastructure/Projections/PasskeyProjection.cs`
  - Confirm Patch operations work with private setters
  - Run associated tests to verify
- [ ] T025 [P] [US2] Verify UserManagement projections: `src/modules/Spamma.Modules.UserManagement/Infrastructure/Projections/ApiKeyProjection.cs`
  - Confirm Patch operations work with private setters
  - Run associated tests to verify
- [ ] T026 [P] [US2] Verify DomainManagement projections: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/DomainLookupProjection.cs`
  - Confirm Patch operations work with private setters
  - Run tests: `dotnet test tests/Spamma.Modules.DomainManagement.Tests/Infrastructure/Projections/`
- [ ] T027 [P] [US2] Verify DomainManagement projections: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/SubdomainLookupProjection.cs`
  - Confirm Patch operations work with private setters
  - Run associated tests to verify
- [ ] T028 [P] [US2] Verify DomainManagement projections: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/ChaosAddressLookupProjection.cs`
  - Confirm Patch operations work with private setters
  - Run associated tests to verify
- [ ] T029 [P] [US2] Verify EmailInbox projections: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/EmailLookupProjection.cs`
  - Confirm Patch operations work with private setters
  - Run tests: `dotnet test tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/Projections/`
- [ ] T030 [P] [US2] Verify EmailInbox projections: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/CampaignSummaryProjection.cs`
  - Confirm Patch operations work with private setters
  - Run associated tests to verify
- [ ] T031 [US2] Run full test suite for all modules to confirm no regressions:
  - `dotnet test tests/Spamma.Modules.UserManagement.Tests/`
  - `dotnet test tests/Spamma.Modules.DomainManagement.Tests/`
  - `dotnet test tests/Spamma.Modules.EmailInbox.Tests/`
- [ ] T032 [US2] Create integration test demonstrating Patch operations with immutable readmodels: `tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/UserLookupProjectionImmutabilityIntegrationTest.cs`
  - Event sourced → projection creates readmodel with private setters
  - Another event sourced → projection patches readmodel
  - Verify final state matches expected values
- [ ] T033 [US2] Commit US2 changes: `git commit -m "test: Verify Marten Patch operations work with immutable readmodels"`

**Parallel**: T023-T030 (projection verification) can all run in parallel as they verify different modules

**Acceptance Test**:
```csharp
// Projection's Patch operation succeeds:
ops.Patch<UserLookup>(streamId)
  .Set(x => x.Name, "updated name")
  .Set(x => x.LastLoginAt, DateTime.UtcNow);
// ^ Compiles and executes without error
```

---

## Phase 5: User Story 3 - Standardize Readmodel Construction (P2)

**Goal**: Document immutable pattern as standard for future readmodels; establish code review checklist

**Acceptance Criteria**:
1. Immutable readmodel pattern document created and reviewed
2. Code review checklist updated to validate immutability
3. Team alignment on future readmodel implementations
4. No new readmodels violate the pattern

**Independent Test Criteria**:
- Pattern documentation is clear and unambiguous
- Code review checklist is specific and actionable
- New test readmodels follow pattern without corrections

### Tasks

- [ ] T034 [US3] Create immutable readmodel pattern guide: `specs/002-immutable-readmodels/patterns/immutable-readmodels.md`
  - Document the pattern: why immutable, when applicable, code examples
  - Include DO/DON'T examples
  - Link to this feature's implementation as reference
- [ ] T035 [US3] Create code review checklist template: `.github/templates/immutable-readmodel-review.md`
  - Checkbox: All properties have private setters
  - Checkbox: Collections initialized to empty `{ get; } = new()`
  - Checkbox: No public constructors with parameters
  - Checkbox: Object initializers work in projection Create methods
  - Checkbox: Patch operations verified compatible
- [ ] T036 [US3] Update project README or architecture guide: `docs/architecture.md` (if exists)
  - Add section: "ReadModels & Immutability Pattern"
  - Reference the pattern guide
  - Link to examples in codebase
- [ ] T037 [US3] Create example readmodel for new developers: `specs/002-immutable-readmodels/examples/ExampleReadModel.cs`
  - Full working example of immutable readmodel
  - Comments explaining each pattern choice
- [ ] T038 [US3] Review and validate pattern documentation with team lead/reviewer
- [ ] T039 [US3] Commit US3 changes: `git commit -m "docs: Establish immutable readmodel pattern standards"`

**Parallel**: T034-T037 can run in parallel; T038 (review) runs after all complete

---

## Phase 6: User Story 4 - Maintain Backward Compatibility (P2)

**Goal**: Verify immutable readmodels deserialize existing Marten documents correctly; no data migration needed

**Acceptance Criteria**:
1. Existing Marten-stored readmodel documents load without errors
2. Deserialization populates all properties correctly from JSON
3. Queries return same data as before
4. Projection replays work end-to-end
5. No database migrations required

**Independent Test Criteria**:
- Existing test documents deserialize and hydrate all properties
- Queries on existing documents return correct results
- Projection replay on existing events succeeds
- No schema changes required

### Tasks

- [ ] T040 [US4] Create backward compatibility test: `tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/UserLookupBackwardCompatibilityTest.cs`
  - Create test readmodel document as if stored by old code (public setters)
  - Verify new immutable readmodel deserializes this JSON correctly
  - Verify all properties hydrate correctly
- [ ] T041 [P] [US4] Create backward compatibility test for DomainManagement: `tests/Spamma.Modules.DomainManagement.Tests/Infrastructure/Projections/DomainLookupBackwardCompatibilityTest.cs`
- [ ] T042 [P] [US4] Create backward compatibility test for EmailInbox: `tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/Projections/EmailLookupBackwardCompatibilityTest.cs`
- [ ] T043 [US4] Run backward compatibility tests; verify all pass
- [ ] T044 [US4] Create projection replay test demonstrating events replay to immutable readmodels correctly: `tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/ProjectionReplayWithImmutableReadmodelsTest.cs`
  - Create sequence of events
  - Replay projection
  - Verify final readmodel state matches expected
  - Verify all Patch operations updated correctly
- [ ] T045 [US4] Run integration test against live PostgreSQL database (docker-compose):
  - Confirm existing readmodel documents deserialize
  - Confirm queries return correct results
  - Confirm projection replay works
- [ ] T046 [US4] Document backward compatibility validation: `specs/002-immutable-readmodels/BACKWARD-COMPATIBILITY.md`
  - Test results
  - Verification checklist
  - Zero migrations required
- [ ] T047 [US4] Commit US4 changes: `git commit -m "test: Verify backward compatibility with immutable readmodels"`

**Parallel**: T041-T042 can run concurrently; T043 after all tests created

---

## Phase 7: Polish & Cross-Cutting Concerns

**Goal**: Final validation, documentation updates, and readiness for code review

**Independent Test Criteria**:
- Full solution builds with zero warnings
- All tests pass (unit + integration)
- Documentation complete and accurate
- Code review checklist prepared

### Tasks

- [ ] T048 Run full solution build with strict warnings: `dotnet build Spamma.sln /p:TreatWarningsAsErrors=true`
  - Verify zero compiler warnings introduced
  - Fix any warnings if detected
- [ ] T049 Run full test suite: `dotnet test tests/`
  - Verify all tests pass
  - Verify no test regressions
- [ ] T050 Code analysis: Run StyleCop on all modified files
  - Verify no StyleCop violations introduced
  - Specifically check SA1206 (public setters are caught)
- [ ] T051 Update main documentation: `README.md` or architecture guide
  - Add note about immutable readmodels implementation
  - Link to feature spec and implementation details
- [ ] T052 Create PR description template: `specs/002-immutable-readmodels/PR-DESCRIPTION.md`
  - Summary of changes
  - Why immutable readmodels matter
  - Testing performed
  - Breaking changes: NONE
  - Migration steps: NONE
- [ ] T053 Update spec with implementation completion date and final notes: `specs/002-immutable-readmodels/spec.md`
  - Add "Completed": [DATE]
  - Add implementation notes section
- [ ] T054 Create PR to merge `002-immutable-readmodels` to main branch
  - Include PR description from T052
  - Reference all test evidence
  - Request code review
- [ ] T055 Final documentation review and Polish: Verify all specs, plans, research, data model, and quickstart are accurate and complete

---

## Dependency Graph

```
Phase 1 (Setup & Validation Spike)
    ↓
Phase 2 (Foundational Work)
    ↓
+---────────────────────────────────────────────────────────────+
│                                                               │
Phase 3 (US1: Immutability) ←→ Phase 4 (US2: Patching) [Parallel P1]
│                                                               │
│   (Can run in parallel; no file conflicts)                   │
│                                                               │
└───────────────────┬───────────────────┬───────────────────────┘
                    ↓                   ↓
            +───────────────────────────────────────+
            │                                       │
        Phase 5 (US3: Standardize)  ←→  Phase 6 (US4: Compatibility) [Parallel P2]
        │                                   │
        │   (Can run in parallel)          │
        │                                   │
        └───────────────┬───────────────────┘
                        ↓
                Phase 7 (Polish)
```

---

## Parallel Execution Examples

### Example 1: Full Parallel MVP (Days 1-2)
```
Day 1:
  Morning: Phase 1 & 2 (Validation spike + foundational work) [2 hours]
  Afternoon: Phase 3 & 4 parallel (All readmodels + all projections) [3 hours]

Day 2:
  Morning: Verify Phase 3 & 4 builds and tests pass [1 hour]
  Afternoon: Phase 5 & 6 parallel (Documentation + compatibility) [2 hours]
  End of day: Phase 7 Polish [1 hour]
```

### Example 2: Sequential but Grouped
```
Sprint 1:
  - Phase 1 & 2 (Setup + foundational)
  - Phase 3 (US1: Immutability)
  - Phase 4 (US2: Patching) — Can start immediately after Phase 1

Sprint 2:
  - Phase 5 (US3: Standardize)
  - Phase 6 (US4: Compatibility) — Can start immediately after Phase 4
  - Phase 7 (Polish)
```

---

## Task Checklist Format

Every task follows the format: `- [ ] [TaskID] [Parallelizable?] [UserStory?] Description`

- **Checkbox**: Markdown `[ ]` for tracking
- **TaskID**: T001-T055 (sequential execution order)
- **[P]**: Parallelizable with other [P] tasks in same phase
- **[US#]**: User story (US1, US2, US3, US4) - only for story-specific tasks
- **Description**: Specific action with file paths

---

## Acceptance Criteria by User Story

### ✅ US1: Enforce Immutability (P1)
- All 9 readmodels have private setters
- Attempting to set property outside class = compile error
- Build passes with zero warnings

### ✅ US2: Enable Marten Patching (P1)
- All projections work with immutable readmodels
- Patch operations successfully update all property types
- All projection tests pass; no regressions

### ✅ US3: Standardize Construction (P2)
- Pattern documented and reviewed
- Code review checklist created
- Team aligned on future implementations

### ✅ US4: Maintain Compatibility (P2)
- Existing documents deserialize without errors
- Queries return same results as before
- Projection replays work end-to-end
- Zero migrations required

---

## Suggested MVP Scope

**Minimum Viable Product (MVP)**: US1 + US2 only

**MVP Effort**: ~4-5 hours (can execute in 1 day)

**MVP Deliverables**:
- All readmodels converted to private setters
- Marten Patch compatibility validated
- All tests passing
- No regressions

**Post-MVP** (US3 + US4): ~2-3 hours

**Benefits of MVP-first approach**:
1. Core functionality (immutability + patching) proven quickly
2. Validation spike discovers any Marten issues immediately
3. Team can review immutability changes before standardization
4. Backward compatibility verified separately

---

## Implementation Notes

### Code Conversion Pattern

**Before (Public Setters)**:
```csharp
public string Name { get; set; } = string.Empty;
public List<Guid> DomainIds { get; set; } = new();
```

**After (Immutable)**:
```csharp
public string Name { get; private set; } = string.Empty;
public List<Guid> DomainIds { get; } = new();
```

### Projection Pattern (No Changes Needed)

**Works as-is with immutable readmodels**:
```csharp
public ReadModel Create(Event @event)
{
    return new ReadModel
    {
        Name = @event.Name,
        Items = new List<T>()
    };
}

// Patch still works because reflection bypasses visibility:
ops.Patch<ReadModel>(id)
    .Set(x => x.Name, "new value");
```

### Testing Strategy

1. **Unit Tests**: Verify compile-time immutability (T021, T040-T042)
2. **Integration Tests**: Verify Patch operations and event sourcing (T032, T044)
3. **Backward Compatibility Tests**: Verify existing documents deserialize (T040-T046)
4. **Regression Tests**: Run existing projection tests without modification (T031)

---

## Success Metrics

| Metric | Target | Validation |
|--------|--------|-----------|
| Public setters on readmodels | 0 (zero) | Code analysis + build check |
| Compiler warnings | 0 (zero) | Build output validation |
| Test pass rate | 100% | Test suite execution |
| Projection compatibility | 100% | All projection tests pass |
| Backward compatibility | 100% | Existing documents deserialize |

---

## Time Estimates

| Phase | Effort | Notes |
|-------|--------|-------|
| Phase 1 (Setup) | 30 min | Validation spike quick test |
| Phase 2 (Foundational) | 45 min | Documentation and enumeration |
| Phase 3 (US1) | 60 min | 9 readmodel conversions (parallelizable) |
| Phase 4 (US2) | 90 min | Projection verification + tests |
| Phase 5 (US3) | 60 min | Pattern documentation |
| Phase 6 (US4) | 90 min | Compatibility testing |
| Phase 7 (Polish) | 45 min | Final validation and PR prep |
| **TOTAL** | **~6-7 hours** | MVP 4-5 hrs, full 6-7 hrs |

---

## Next Steps

1. **Review this task list** with team
2. **Execute Phase 1** (validation spike) to confirm Marten compatibility
3. **Decide on scope**: MVP (US1+US2) vs Full (all 4 stories)
4. **Assign tasks**: Use parallelization opportunities (T012-T019, T023-T030, T041-T042)
5. **Track progress**: Use checklist to mark tasks as complete
6. **Code review**: Upon completion, create PR for team review

---

## Revision History

| Date | Version | Notes |
|------|---------|-------|
| 2025-11-17 | 1.0 | Initial task breakdown from `/speckit.tasks` workflow |
