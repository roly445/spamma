# Feature Specification: Bug sweep — minor bugs & niggles

**Feature Branch**: `001-bug-sweep`  
**Created**: 2025-11-05  
**Status**: Draft  
**Input**: User description: "I want to perform a sweep up of some minor bugs ang gniggles that have been introduced"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fix blocking UI glitches (Priority: P1)

As an end user, I expect the application UI to behave consistently without visual glitches, broken layouts, or interactive elements that fail to respond.

Why this priority: Visual and interactive bugs negatively impact usability and confidence; fixing them restores core user flows.

Independent Test: Manually exercise core UI pages (login/setup/dashboard/domain inbox) and verify layout, buttons, and forms render and respond correctly. Automated UI smoke tests should pass for these pages.

Acceptance Scenarios:

1. **Given** the application is running, **When** a user navigates to the login or setup pages, **Then** layout elements render within viewport and no interactive controls are non-responsive.
2. **Given** a dashboard or list view, **When** the user interacts with primary actions (click, input, submit), **Then** the expected client-side action occurs and no JavaScript errors are logged.

---

### User Story 2 - Fix backend error paths that surface to users (Priority: P2)

As an end user, I want meaningful, non-technical error messages instead of stack traces or internal errors when recoverable issues occur.

Why this priority: Clear error messaging prevents confusion and reduces support load.

Independent Test: Trigger known recoverable failures (e.g., invalid setup values, missing SMTP) and verify the UI shows friendly error messages and the server returns appropriate status codes.

Acceptance Scenarios:

1. **Given** an operation fails due to a recoverable cause, **When** the user performs the operation, **Then** the UI displays a clear, actionable message and no raw exception details.

---

### User Story 3 - Fix small data/processing inconsistencies (Priority: P3)

As an operator, I want small inconsistencies (timestamps, counts, status labels) corrected so reports and projections reflect accurate state.

Why this priority: Accurate read models and metadata improve operational clarity and reduce confusing behaviour.

Independent Test: Run projection/query flows and compare counts/timestamps against expected outcomes after creating sample data; unit tests for projection logic should pass.

Acceptance Scenarios:

1. **Given** a newly received email or domain change, **When** projections update, **Then** read-models show the expected status and timestamps within accepted tolerance (seconds-level accuracy for created/received timestamps).

---

### Edge Cases

- What happens when a user has intermittent network connectivity? UI should gracefully retry or surface clear guidance.
- How does system behave when a background projection fails? It should log an actionable error and retry without blocking other features.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Developers MUST identify and fix visible UI layout issues on login, setup, and primary client pages so controls render and respond correctly across common viewport sizes.
- **FR-002**: Server responses for recoverable errors MUST return user-friendly messages with appropriate HTTP status codes; raw exception details MUST NOT be returned to clients.
- **FR-003**: Projections and read-models MUST reflect the correct state after domain events; obvious mismatches in counts or status labels MUST be corrected.
- **FR-004**: JavaScript console MUST not contain unhandled exceptions during normal flows (login, setup, inbox viewing) in manual smoke tests.
- **FR-005**: Fixes MUST include unit or integration tests where feasible (projection fixes, command handler fixes, critical client-side logic) to prevent regressions.

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code changes included in the PR MUST compile cleanly with zero new warnings.
- **CQ-002**: Any new public types MUST follow repository naming and One-Type-Per-File rules from StyleCop guidance in `Spamma.Shared`.
- **CQ-003**: Frontend interactive components MUST remain split into `.razor` and `.razor.cs` when applicable.
- **CQ-004**: Include tests for any corrected projection, query, or handler logic where reasonable; add mocks/stubs for external dependencies.
- **CQ-005**: Document each change in the PR description with a brief explanation and link to failing behavior it resolves.

### Key Entities *(include if feature involves data)*

- **ReadModel / Projection**: Representation of computed state (counts, statuses, timestamps) that must be validated after fixes.
- **UI Page / Component**: Named interactive pages (Login, Setup, Inbox, Dashboard) where layout and interactivity must be verified.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No high-severity UI regressions remain in core flows (login, setup, inbox, dashboard) as verified by a 10-item UI smoke test checklist executed manually and automatically.
- **SC-002**: 0 occurrences of raw exception details shown to users in tested flows; all errors surfaced are user-friendly messages.
- **SC-003**: Projection/read-model count mismatches reduced to zero for the tested scenarios (sample dataset of 100 items) compared to current baseline.
- **SC-004**: Automated tests (unit/integration) covering fixed areas are added and pass in CI with at least 90% success rate for the new tests.

