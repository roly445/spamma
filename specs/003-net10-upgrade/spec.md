# Feature Specification: .NET 10 Upgrade

**Feature Branch**: `003-net10-upgrade`  
**Created**: 2025-11-17  
**Status**: Draft  
**Input**: User description: "Update the application to use .NET 10 and update the NuGet packages to their .NET 10 versions"

## Clarifications

### Session 2025-11-17

- Q: How should compiler warnings from upgraded dependencies be handled? → A: Update all APIs: fix every warning in transitive dependencies or replace packages before upgrading (ensures zero warnings but higher effort).
- Q: What NuGet package version strategy should be used for .NET 10 compatibility? → A: Target latest stable minor version compatible with .NET 10 (e.g., current major series latest stable patch/minor).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developers Can Build and Run Application on .NET 10 (Priority: P1)

Developers need to successfully build the entire Spamma solution (all 8+ server modules, client modules, and test projects) targeting .NET 10 runtime without errors or warnings. This includes both local development builds and CI/CD pipeline execution.

**Why this priority**: This is the foundational requirement—without a successful build, nothing else works. All development, testing, and deployment depends on this.

**Independent Test**: A developer can clone the repository, install .NET 10 SDK, run `dotnet build Spamma.sln --no-restore` and achieve a clean build with zero errors and zero new warnings.

**Acceptance Scenarios**:

1. **Given** .NET 10 SDK is installed, **When** running `dotnet build Spamma.sln`, **Then** all projects compile successfully with no errors
2. **Given** a clean build has completed, **When** inspecting the build output, **Then** no new compiler warnings appear (only pre-existing suppressions)
3. **Given** .NET 9 binaries exist, **When** running `dotnet clean Spamma.sln` followed by rebuild, **Then** all projects target .NET 10 runtime

---

### User Story 2 - All Unit and Integration Tests Pass on .NET 10 (Priority: P1)

The entire test suite (UserManagement, DomainManagement, EmailInbox, and common tests) must execute successfully on .NET 10 without any test failures or runtime incompatibilities.

**Why this priority**: Tests validate the application logic. If tests fail after upgrade, we cannot ensure the application behavior is preserved.

**Independent Test**: Running `dotnet test tests/ --no-restore` executes all test projects successfully with all tests passing (same pass rate as on .NET 9).

**Acceptance Scenarios**:

1. **Given** tests are targeting .NET 10, **When** running the full test suite, **Then** all previously passing tests continue to pass
2. **Given** a test failure occurs, **When** investigating, **Then** the root cause is identified (e.g., dependency incompatibility) and documented
3. **Given** .NET 10-specific deprecated APIs are encountered, **When** tests run, **Then** they handle deprecation warnings or are updated to use new APIs

---

### User Story 3 - Application Starts and Serves Pages on .NET 10 Runtime (Priority: P1)

The main Spamma application (`Spamma.App`) can start successfully, load the database migrations, initialize event sourcing infrastructure (Marten), and serve both static setup pages and Blazor WebAssembly components without runtime errors.

**Why this priority**: This validates that the application can actually run in production. Setup pages and authentication flow must work from day one.

**Independent Test**: Running `dotnet run --project src/Spamma.App/Spamma.App` (with Docker services running) starts the application, displays setup page, and doesn't crash on initialization.

**Acceptance Scenarios**:

1. **Given** Docker Compose services are running (PostgreSQL, Redis), **When** the application starts, **Then** it connects to the database and initializes Marten event sourcing without errors
2. **Given** the application is running, **When** navigating to the home page, **Then** the Blazor setup page loads and renders without JavaScript console errors
3. **Given** authenticated requests are made, **When** Blazor WebAssembly components query the API, **Then** CQRS handlers execute successfully using .NET 10 features

---

### User Story 4 - All NuGet Dependencies Are Compatible with .NET 10 (Priority: P2)

Core dependencies (MediatR, Marten, CAP, FluentValidation, etc.) are updated to versions that support .NET 10, and no runtime compatibility issues exist between transitive dependencies.

**Why this priority**: Dependency compatibility ensures long-term maintainability and security. Out-of-date packages can have vulnerabilities. However, this is secondary to getting the build working.

**Independent Test**: Running `dotnet list package --vulnerable` shows no vulnerabilities in the updated package set, and `dotnet build` completes without version conflict warnings.

**Acceptance Scenarios**:

1. **Given** NuGet packages are updated, **When** resolving package versions, **Then** no version conflicts exist (no "A depends on B v2.0, but C depends on B v1.0" issues)
2. **Given** running the application, **When** executing queries and commands, **Then** MediatR, Marten, and CAP work correctly with their .NET 10-compatible versions
3. **Given** Blazor WebAssembly components are compiled, **When** their JavaScript is executed, **Then** no compatibility issues arise from updated Blazor packages

---

### User Story 5 - Docker Build Image Uses .NET 10 Runtime (Priority: P2)

The Docker image for the application is updated to use the .NET 10 runtime base image, reducing deployment friction and enabling production deployments on .NET 10.

**Why this priority**: While important for production readiness, local development can continue without Docker immediately after the upgrade. CI/CD will catch this if missed.

**Independent Test**: Building the Docker image with `docker build -f Dockerfile .` (if Dockerfile exists) succeeds and uses .NET 10, or docker-compose.yml is verified to reference .NET 10 compatible base images.

**Acceptance Scenarios**:

1. **Given** a Docker build is initiated, **When** the build completes, **Then** the resulting image contains .NET 10 runtime
2. **Given** a container is started from the image, **When** running the application, **Then** all features work identically to local .NET 10 execution

---

### User Story 6 - CI/CD Pipeline Passes with .NET 10 (Priority: P2)

The GitHub Actions CI/CD workflow (`.github/workflows/ci.yml`) successfully builds, tests, and validates the application using .NET 10 SDK without failures.

**Why this priority**: Ensures that all pull requests and releases use .NET 10 going forward. Secondary to local development but essential for team collaboration.

**Independent Test**: Pushing a commit to any branch triggers the CI workflow, and it completes successfully (build passes, all tests pass, security scans pass).

**Acceptance Scenarios**:

1. **Given** GitHub Actions workflow runs, **When** the build step executes, **Then** it uses .NET 10 SDK and completes without errors
2. **Given** tests run in CI, **When** they complete, **Then** the same tests that pass locally also pass in CI
3. **Given** code quality checks run, **When** StyleCop and SonarQube analysis complete, **Then** no new quality issues are introduced by the upgrade

---

### Edge Cases

- What happens if a NuGet package doesn't have a .NET 10-compatible version and must be replaced or removed?
- How are deprecated .NET 9 APIs handled if code still references them (should they be updated or wrapped)?
- What if Blazor WebAssembly compilation requires additional configuration changes for .NET 10?
- What if Docker base images don't support the target .NET 10 version at the time of upgrade?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST compile all projects (server modules, client modules, tests, samples, tools) targeting .NET 10 TFM (Target Framework Moniker)
- **FR-002**: System MUST update `global.json` SDK version field from 9.0.0 to 10.0.0 with `rollForward: latestMajor` strategy
- **FR-003**: System MUST update all `csproj` files to target `.NET10` (or `net10`) instead of `.NET9` or `net9`
- **FR-004**: System MUST update NuGet package references to versions compatible with .NET 10 targeting the latest stable minor/patch version within the current major version series (e.g., if currently on MediatR v12.x, update to latest v12.x stable; do not jump to MediatR v13.x unless required for .NET 10 compatibility)
- **FR-004a**: When package upgrades introduce breaking changes requiring major version bumps, those changes MUST be reflected in the codebase (e.g., if MediatR v12.x has no .NET 10 support but v13.x does, upgrade to v13.x and update calling code accordingly)
- **FR-005**: System MUST pass all unit tests (Spamma.*.Tests projects) targeting .NET 10 runtime without any test failures
- **FR-006**: System MUST pass all integration tests (Spamma.*.Tests.E2E projects) on .NET 10 runtime
- **FR-007**: Application MUST initialize successfully on .NET 10, including Marten event sourcing, CAP message queue, and PostgreSQL connection
- **FR-008**: Blazor WebAssembly components MUST compile correctly for .NET 10 and execute without JavaScript/interop errors
- **FR-009**: System MUST handle any breaking changes in dependency libraries (e.g., API surface changes, behavioral changes) by updating calling code
- **FR-009a**: When NuGet package upgrades introduce compiler warnings in our project, all such warnings MUST be addressed by updating the calling code to use non-deprecated APIs (not suppressed)
- **FR-010**: Docker image(s) MUST be updated to use .NET 10 runtime base image (if Docker is used for deployment)
- **FR-011**: CI/CD workflows MUST execute using .NET 10 SDK for build, test, and validation steps
- **FR-012**: System MUST compile with zero new compiler warnings (existing suppressions in GlobalSuppressions.cs are acceptable)

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code MUST compile with zero new warnings. StyleCop and SonarQube analysis must pass with no regression.
- **CQ-002**: No breaking changes to public APIs should occur as a result of the .NET 10 upgrade (unless absolutely necessary and documented).
- **CQ-003**: Deprecated APIs from .NET 9 MUST be replaced with their .NET 10 equivalents where identified during compilation or code review.
- **CQ-004**: All project files (`*.csproj`) MUST be consistent in their .NET version targeting (no mixed net9/net10 projects unless explicitly needed).
- **CQ-005**: NuGet package versions MUST be documented in a summary list (created as reference documentation for the PR) including: package name, .NET 9 version, .NET 10 version, and notes on any breaking changes requiring code updates or major version bumps.

### Key Entities *(N/A - No new data models required)*

This upgrade does not introduce new domain entities. All existing entities and aggregates remain unchanged—only the runtime target changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Solution builds successfully with `dotnet build Spamma.sln --no-restore` on .NET 10 SDK in under 2 minutes (or equal to .NET 9 build time)
- **SC-002**: All unit tests pass with 100% success rate (maintaining the same test pass count as .NET 9 baseline)
- **SC-003**: Application starts and serves the home page within 10 seconds of running `dotnet run` (same performance baseline as .NET 9)
- **SC-004**: Zero runtime exceptions or crashes when loading setup pages, authenticating, or querying CQRS handlers
- **SC-005**: CI/CD pipeline completes build and test stages in under 10 minutes (same duration as .NET 9 CI baseline)
- **SC-006**: No new security vulnerabilities introduced (verified via `dotnet list package --vulnerable`)
- **SC-007**: Code coverage metrics remain at or above .NET 9 baseline (no coverage regression)
- **SC-008**: Blazor WebAssembly bundle size remains comparable to .NET 9 (no more than 5% increase in final artifact size)

## Assumptions & Constraints

### Assumptions

- .NET 10 SDK is available via official Microsoft distribution channels (no custom builds needed)
- All dependencies have or will receive .NET 10-compatible versions during the upgrade period
- No breaking changes in .NET 9 → .NET 10 migration require extensive code refactoring (minor updates only)
- Docker infrastructure supports .NET 10 runtime (official `mcr.microsoft.com/dotnet` images are used)
- Existing database schema for Marten event sourcing is forward-compatible with .NET 10 (no migrations required)

### Constraints

- The upgrade must not delay active feature development (should be completed within one sprint)
- No new features are added as part of this upgrade (scope limited to runtime and dependency updates only)
- Rollback capability must be maintained (feature branch can be reverted if critical issues surface)
- All team members must have .NET 10 SDK available locally before PR merge (documented in README)

## Out of Scope

- Performance tuning or optimization (performance should match .NET 9)
- New feature additions or design changes
- Database schema migrations (only if required by Marten updates)
- Breaking API changes (unless absolutely necessary for .NET 10 compatibility)
- Upgrade of Azure DevOps or CI/CD infrastructure beyond SDK version updates

## Dependencies & Related Work

- Requires .NET 10 SDK availability on developer machines
- Depends on NuGet package maintainers providing .NET 10-compatible releases
- May overlap with any ongoing dependency security updates
- Should be coordinated with any Blazor or ASP.NET Core feature work in progress

## Definition of Done

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
