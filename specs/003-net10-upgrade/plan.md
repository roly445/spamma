# Implementation Plan: .NET 10 Upgrade

**Branch**: `003-net10-upgrade` | **Date**: 2025-11-17 | **Spec**: [specs/003-net10-upgrade/spec.md](spec.md)
**Input**: Feature specification from `/specs/003-net10-upgrade/spec.md`

## Summary

**Primary Requirement**: Upgrade Spamma modular monolith from .NET 9 to .NET 10 runtime, including updates to all NuGet dependencies targeting latest stable minor/patch versions compatible with .NET 10.

**Technical Approach**:

1. Update global.json SDK version and all project file target frameworks
2. Update NuGet package versions to .NET 10-compatible releases
3. Fix any compiler warnings and API deprecations introduced by dependency updates
4. Validate all tests pass and application runs successfully
5. Update CI/CD pipeline and Docker configuration for .NET 10

## Technical Context

**Language/Version**: C# with .NET 10 (currently .NET 9)

**Primary Dependencies**:

- MediatR (CQRS command/query handling)
- Marten (PostgreSQL event sourcing)
- CAP (distributed transaction & messaging with Redis)
- FluentValidation (command/query validation)
- Blazor WebAssembly (client-side UI)
- xUnit + Moq (testing)
- PostgreSQL (event store)
- Redis (message queue)

**Storage**: PostgreSQL (via Marten event store) + Redis (CAP message queue)

**Testing**: xUnit + Moq for unit/integration tests (6+ test projects)

**Target Platform**: ASP.NET Core server + Blazor WebAssembly frontend (modular monolith)

**Project Type**: Web application (modular monolith with 8+ server modules + client projects)

**Performance Goals**: Build completes in ≤2 minutes (parity with .NET 9); app startup ≤10 seconds; tests complete in baseline time

**Constraints**:

- Must not introduce new compiler warnings
- All tests must pass with 100% success rate
- No breaking API changes (unless documented)
- Complete within one sprint
- Maintain code coverage baseline

**Scale/Scope**:

- 8+ server modules (UserManagement, DomainManagement, EmailInbox, Common, etc.)
- 6+ client modules
- 6 test projects
- ~30-50 NuGet dependencies (direct + transitive)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The following checks MUST be validated and documented in the plan before research proceeds:

- **Tests**: ✅ PASS
  - Existing unit tests for domain logic will be re-run on .NET 10
  - No new tests required (upgrade validates existing functionality)
  - All 5+ test projects must pass with 100% success rate
  - Integration tests exercise Marten, CAP, PostgreSQL connections
  
- **Observability**: ✅ PASS
  - No new observability required (upgrade maintains existing logging/tracing)
  - Existing structured logging remains unchanged
  - CAP integration events continue to be logged
  - Build system and test output monitored for warnings
  
- **Security & Privacy**: ✅ PASS
  - No security model changes (upgrade maintains existing RBAC/TLS/STARTTLS)
  - No data retention implications
  - No transport security changes required
  - Dependency vulnerability scan required pre/post-upgrade
  
- **Code Quality**: ✅ PASS
  - ✅ Zero compiler warnings (FR-012, CQ-001)
  - ✅ No XML documentation comments required (StyleCop SA1600 suppressed)
  - ✅ Blazor components already split into .razor + .razor.cs (no changes needed)
  - ✅ Commands/Queries in .Client projects; handlers in server projects (no changes)
  - ✅ No new projects being added
  - ✅ Deprecated .NET 9 APIs replaced with .NET 10 equivalents (FR-009a)
  - ✅ All warnings from dependency upgrades addressed via API updates (FR-009a, Clarification Q1)
  
- **CI Compatibility**: ✅ PASS
  - GitHub Actions workflow must use .NET 10 SDK
  - Headless build of frontend assets must succeed
  - All automated checks must pass locally before PR
  - Dockerfile (if present) must reference .NET 10 base image

**Re-evaluation Post-Phase 1**: Will confirm no Constitution violations remain after design phase.

## Project Structure

### Documentation (this feature)

```text
specs/003-net10-upgrade/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (dependency research & breaking changes)
├── data-model.md        # Phase 1 output (N/A - upgrade only, no data model changes)
├── quickstart.md        # Phase 1 output (upgrade validation instructions)
├── contracts/           # Phase 1 output (N/A - no new contracts)
└── checklists/
    └── requirements.md  # Specification quality checklist
```

### Source Code Structure (no changes to layout)

The .NET 10 upgrade does not change any source code structure. Existing modules and projects remain in place:

```text
src/
├── modules/
│   ├── Spamma.Modules.UserManagement/
│   ├── Spamma.Modules.UserManagement.Client/
│   ├── Spamma.Modules.DomainManagement/
│   ├── Spamma.Modules.DomainManagement.Client/
│   ├── Spamma.Modules.EmailInbox/
│   ├── Spamma.Modules.EmailInbox.Client/
│   ├── Spamma.Modules.Common/
│   └── Spamma.Modules.Common.Client/
├── Spamma.App/                    # Main server (target: net10)
└── Spamma.App.Client/             # Blazor WASM client (target: net10)

shared/
└── Spamma.Shared/                 # Shared StyleCop config (no changes)

tests/
├── Spamma.Tests.Common/           # Common test utilities (target: net10)
├── Spamma.Modules.UserManagement.Tests/        (target: net10)
├── Spamma.Modules.DomainManagement.Tests/      (target: net10)
├── Spamma.Modules.EmailInbox.Tests/            (target: net10)
├── Spamma.Modules.EmailInbox.Tests.E2E/        (target: net10)
└── Spamma.App.Tests/                           (target: net10)

samples/
└── Spamma.Samples.GrpcEmailPushClient/         (target: net10)

tools/
└── Spamma.Tools.EmailLoadTester/               (target: net10)
```

**Structure Decision**: Existing modular monolith structure is preserved. All files retain their locations; only `.csproj` TargetFramework values and dependency versions change. No projects are added or removed.

## Complexity Tracking

> No Constitution Check violations identified. This is a straightforward upgrade with no architectural changes.

| Item | Status | Notes |
|------|--------|-------|
| New projects required | ✅ None | All existing projects remain in place |
| Architecture changes | ✅ None | Modular monolith structure preserved |
| Breaking API changes | ✅ To be determined | Will be documented if found during Phase 0 research |
| New entity types | ✅ None | No domain model changes |
| Dependency conflicts | ❓ To be researched | Phase 0 will identify version conflicts |

---

## Phase 0: Research

**Purpose**: Research .NET 9 → .NET 10 migration path, identify all dependencies needing updates, document breaking changes.

**Deliverables**:

1. **research.md** containing:
   - .NET runtime migration guide for Spamma's dependencies
   - Current dependency versions (MediatR, Marten, CAP, FluentValidation, xUnit, etc.)
   - Target versions for .NET 10 compatibility
   - Breaking changes requiring code updates
   - Migration path for each major version bump (if applicable)
   - Deprecated APIs in .NET 9 code that need updating
   - Best practices for handling compiler warnings from upgraded packages

2. **Dependency Audit Spreadsheet** (included in research.md):
   - Package name | Current version | Target version | Breaking changes | Migration notes | Priority

**Research Tasks**:

1. **Task R1**: Analyze .NET 9 → .NET 10 runtime breaking changes (Microsoft docs)
2. **Task R2**: Audit current NuGet packages and identify compatible .NET 10 versions
3. **Task R3**: Research MediatR, Marten, CAP, FluentValidation version compatibility
4. **Task R4**: Identify deprecated APIs in Spamma codebase that need updating
5. **Task R5**: Document breaking changes and migration strategy per package
6. **Task R6**: Establish "latest stable minor in current major" version strategy applicability per package

**Success Criteria**:
- ✅ All NEEDS CLARIFICATION items resolved
- ✅ Complete dependency compatibility matrix created
- ✅ Breaking changes documented with migration approach
- ✅ No ambiguity about API deprecations

---

## Phase 1: Design & Validation

**Prerequisites**: research.md complete

**Purpose**: Design the upgrade steps and validate against Spamma's codebase.

### Step 1.1: Create data-model.md

**Note**: This step is N/A for the .NET 10 upgrade as no data models change.

**Placeholder**:
```markdown
# Data Model: .NET 10 Upgrade

## Summary

The .NET 10 upgrade does not introduce new domain entities or data models. All existing aggregates, events, and value objects remain unchanged.

## Existing Entities (No Changes)

- User (Spamma.Modules.UserManagement domain)
- Domain/Subdomain (Spamma.Modules.DomainManagement domain)
- EmailMessage (Spamma.Modules.EmailInbox domain)
- Integration events (Spamma.Modules.Common.IntegrationEvents)

## Database Schema

- Marten event store schema: forward-compatible with .NET 10 (no migrations needed)
- PostgreSQL version requirements: unchanged
- Redis message queue: unchanged

## Validation Rules

- All existing validation rules remain unchanged
- FluentValidation rules will be updated only to fix breaking changes in FluentValidation itself
```

### Step 1.2: Generate API Contracts

**Note**: This step is N/A for the .NET 10 upgrade as no new APIs are created.

### Step 1.3: Create quickstart.md

**Deliverable**: Validation instructions for testing .NET 10 upgrade locally.

**Contents**:
```markdown
# Quickstart: Validate .NET 10 Upgrade

## Prerequisites

- .NET 10 SDK installed
- Docker Compose (for PostgreSQL + Redis)
- Git repository cloned to latest 003-net10-upgrade branch

## Local Validation Steps

1. **Install .NET 10 SDK**
   ```powershell
   dotnet --version  # Should show 10.x.x
   ```

2. **Start infrastructure**
   ```powershell
   docker-compose up -d
   ```

3. **Verify build**
   ```powershell
   dotnet build Spamma.sln --no-restore
   # Expected: All projects build with zero new warnings
   ```

4. **Run all tests**
   ```powershell
   dotnet test tests/ --no-restore
   # Expected: All tests pass with 100% success rate
   ```

5. **Start application**
   ```powershell
   dotnet run --project src/Spamma.App/Spamma.App
   # Expected: App starts without exceptions, setup page loads
   ```

6. **Verify CI/CD**
   - Push to feature branch
   - GitHub Actions workflow runs
   - Expected: Build passes, tests pass, security scans pass

## Rollback

If critical issues arise:
```powershell
git checkout main
dotnet clean Spamma.sln
dotnet build Spamma.sln
```

## Testing Checklist

- [ ] `dotnet build` passes with zero new warnings
- [ ] `dotnet test tests/` passes with 100% test pass rate
- [ ] Application starts within 10 seconds
- [ ] Blazor UI loads without JavaScript console errors
- [ ] Docker image builds successfully
- [ ] CI/CD pipeline completes successfully
- [ ] No new security vulnerabilities detected
```

### Step 1.4: Create contracts/ directory

**Note**: This directory is N/A for the .NET 10 upgrade. No new API contracts are generated.

### Step 1.5: Update Agent Context

After Phase 1 design, run:
```powershell
.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot
```

This ensures the AI agent is aware of:
- .NET 10 as target runtime
- Updated dependency versions
- Any breaking changes affecting implementation

---

## Post-Phase 1 Constitution Re-check

**All gates PASS**:
- ✅ Tests: Existing unit/integration tests validate upgrade
- ✅ Observability: No new observability required
- ✅ Security & Privacy: Upgrade maintains security model
- ✅ Code Quality: Zero new warnings, no API breaks, structure preserved
- ✅ CI Compatibility: All checks runnable in CI environment

---

## Phase 2 Execution Plan (Overview)

*Phase 2 tasks will be generated via `/speckit.tasks` command and detailed in `tasks.md`.*

**High-level Phase 2 breakdown** (6 major areas):

1. **Configuration Updates** (4 tasks)
   - Update global.json to .NET 10
   - Update all csproj files to net10 TFM
   - Update CI/CD workflow for .NET 10
   - Update Docker configuration

2. **Dependency Updates** (2 tasks)
   - Update primary dependencies (MediatR, Marten, CAP, FluentValidation, xUnit)
   - Update transitive dependencies and resolve conflicts

3. **API Deprecation Fixes** (3 tasks)
   - Address deprecated .NET 9 APIs used in codebase
   - Fix compiler warnings from upgraded packages
   - Review and update Blazor-specific code if needed

4. **Testing & Validation** (3 tasks)
   - Run full test suite and fix failures
   - Validate application startup and runtime behavior
   - Verify Docker image builds and runs

5. **Documentation & PR** (2 tasks)
   - Create NuGet dependency version summary for PR
   - Update README.md with .NET 10 requirement
   - Document any breaking changes requiring team awareness

6. **CI/CD & Deployment** (1 task)
   - Verify GitHub Actions workflow passes
   - Ensure all automated checks pass

---

## Success Metrics (Acceptance Gates)

All success criteria from the spec must be met before PR merge:

| Criterion | Target | Verification |
|-----------|--------|--------------|
| SC-001: Build time | ≤2 min (parity with .NET 9) | `time dotnet build` |
| SC-002: Test pass rate | 100% (same as .NET 9) | `dotnet test --no-restore` |
| SC-003: App startup | ≤10 sec (parity with .NET 9) | Manual timing |
| SC-004: Zero runtime exceptions | On setup page & auth flow | Manual testing |
| SC-005: CI/CD duration | ≤10 min (parity with .NET 9) | GitHub Actions log |
| SC-006: No new vulnerabilities | `dotnet list package --vulnerable` output | Security scan |
| SC-007: Code coverage | ≥ .NET 9 baseline | Coverage reports |
| SC-008: Blazor bundle size | ≤5% increase vs .NET 9 | Bundle size tool |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Dependency has no .NET 10 support | Medium | High | Research phase identifies all incompatibilities; plan fork/replacement if needed |
| Breaking changes in key dependencies | Medium | High | Phase 0 research documents all changes; Phase 2 includes time for code updates |
| Compiler warnings flood output | Medium | Medium | FR-009a clarification: update all deprecated APIs (no suppressions) |
| CI/CD workflow breaks | Low | High | Update GitHub Actions workflow in advance; test locally first |
| Blazor WASM compilation fails | Low | High | Phase 2 includes dedicated validation task |
| Performance regression | Low | Medium | Success criteria tracks parity with .NET 9 |

---

## Definition of Done (from Spec)

- [ ] `global.json` updated to .NET 10
- [ ] All `csproj` files targeting net10
- [ ] All NuGet packages updated to .NET 10-compatible versions
- [ ] `dotnet build Spamma.sln` passes with zero new warnings
- [ ] `dotnet test tests/` passes with 100% test success rate
- [ ] Application starts successfully and serves pages
- [ ] Docker image builds and runs correctly (if applicable)
- [ ] CI/CD workflow executes successfully
- [ ] README.md updated with .NET 10 requirement
- [ ] PR description includes list of updated dependencies
- [ ] No breaking API changes in public types (or changes documented)
