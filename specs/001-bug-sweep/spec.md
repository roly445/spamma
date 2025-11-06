# Feature Specification: Bug sweep — minor bugs & niggles

**Feature Branch**: `001-bug-sweep`  
**Created**: 2025-11-05  
**Status**: Draft  
**Input**: User description: "I want to perform a sweep up of some minor bugs ang gniggles that have been introduced"
 
## Clarifications

### Session 2025-11-06

- Q: Campaign-bound email behavior → A: Disallow both deletion and favouriting for campaign-bound emails.

- Q: Error handling style → A: Use BluQube built-in error handling (return failed CommandResult with BluQubeErrorData and error code `EmailIsPartOfCampaign`).

- Q: Admin override policy → A: No override — campaign-bound emails are never deletable or favouritable (no admin/force override).

- Q: Test coverage for campaign protections → A: Unit tests + API contract/integration tests (no browser-level UI test required).

- Q: Campaign protection trigger → A: Protect whenever `CampaignId` is non-null (no campaign lookup).

- Q: Campaign deletion handling → A: Cascade delete: deleting a Campaign also deletes all associated emails.

- Q: Campaign deletion execution → A: Synchronous cascade delete (immediate in same request/transaction).

- Q: HTTP/API mapping for campaign protection errors → A: Use standard BluQube error responses (return failed CommandResult with BluQubeErrorData; do not hard-code HTTP status in tests).

- Q: Campaign email cardinality → A: There is always exactly one email per campaign (1:1). No bulk-delete limits required.

- Q: Client UI behavior for campaign-bound emails → A: Hide Delete/Favorite controls completely (no visible controls).

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

- Campaign-bound email operations: Background services (e.g., `EmailCleanupBackgroundService`) and automated processes that issue `DeleteEmailCommand` or `ToggleEmailFavoriteCommand` MUST respect campaign association and either skip such emails or escalate to manual review. All such actions MUST be recorded in an audit log with the initiating actor and timestamp.
Campaign-bound email operations: Background services (e.g., `EmailCleanupBackgroundService`) and automated processes that issue `DeleteEmailCommand` or `ToggleEmailFavoriteCommand` MUST respect campaign association and either skip such emails or escalate to manual review. All domain-changing actions — including synchronous cascade deletions triggered by deleting a Campaign — MUST be recorded in an auditable event trail (for example, emitted Marten events or a dedicated audit record) that includes the initiating actor and timestamp. This requirement aligns with the project constitution's Observability & Auditability principle.

Clarification: deleting a Campaign WILL cascade-delete the single associated email. Because campaigns are 1:1 with emails, this synchronous cascade is safe and does not require bulk-delete limits. This cascade occurs synchronously in the same HTTP request/transaction (i.e., delete call performs immediate removal). Implementers SHOULD ensure requests are protected from timeouts and that irreversible actions are covered by backups or explicit data-migration procedures. Recording audit logs for cascade deletions IS REQUIRED by this spec; ensure Marten events or equivalent audit records are emitted so the action is auditable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Developers MUST identify and fix visible UI layout issues on login, setup, and primary client pages so controls render and respond correctly across common viewport sizes.
- **FR-002**: Server responses for recoverable errors MUST return user-friendly messages with appropriate HTTP status codes; raw exception details MUST NOT be returned to clients.
- **FR-003**: Projections and read-models MUST reflect the correct state after domain events; obvious mismatches in counts or status labels MUST be corrected.
- **FR-004**: JavaScript console MUST not contain unhandled exceptions during normal flows (login, setup, inbox viewing) in manual smoke tests.
- **FR-005**: Fixes MUST include unit or integration tests where feasible (projection fixes, command handler fixes, critical client-side logic) to prevent regressions.

- **FR-007**: Testing scope for campaign-protection fixes: Implement unit tests for command handlers and validators, and API-level integration/contract tests that verify BluQube error semantics (failed `CommandResult` with `BluQubeErrorData` and `EmailIsPartOfCampaign` code). Browser-level end-to-end UI tests are optional for this change and not required to meet acceptance.


Clarification: API tests should assert on the returned `BluQubeErrorData` and error code (`EmailIsPartOfCampaign`) rather than relying on a particular HTTP status code. The BluQube runtime will map the error data to an appropriate HTTP response.


- **FR-006**: Campaign-bound emails MUST be protected from client-initiated deletion and favouriting. The server MUST validate and reject Delete or ToggleFavorite operations when an Email has a non-null `CampaignId` by returning a failed `CommandResult` containing `BluQubeErrorData` with the error code `EmailIsPartOfCampaign`. The client UI MUST hide or disable Delete and Favorite controls for campaign-bound emails and surface an explanatory tooltip or message when appropriate. There is NO admin/force override for campaign-bound emails; corrective action requires deleting the campaign (which may cascade) or an explicit data-migration process outside normal runtime commands.


Note: There is NO admin/force override for campaign-bound emails. Any corrective action must be performed by deleting the campaign (which may cascade) or via an explicit data-migration process outside normal runtime commands; do not implement `Force` flags or admin bypasses for Delete/ToggleFavorite commands.

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code changes included in the PR MUST compile cleanly with zero new warnings.
- **CQ-002**: Any new public types MUST follow repository naming and One-Type-Per-File rules from StyleCop guidance in `Spamma.Shared`.
- **CQ-003**: Frontend interactive components MUST remain split into `.razor` and `.razor.cs` when applicable.
- **CQ-004**: Include tests for any corrected projection, query, or handler logic where reasonable; add mocks/stubs for external dependencies.
- **CQ-005**: Document each change in the PR description with a brief explanation and link to failing behavior it resolves.

### Key Entities *(include if feature involves data)*

- **ReadModel / Projection**: Representation of computed state (counts, statuses, timestamps) that must be validated after fixes.
- **UI Page / Component**: Named interactive pages (Login, Setup, Inbox, Dashboard) where layout and interactivity must be verified.

- **Data Model (Email)**: Email records SHOULD include a nullable `CampaignId` (GUID) field indicating association to a campaign when present. Server logic will treat non-null `CampaignId` as 'campaign-bound'.


Clarification: protection trigger — any non-null `CampaignId` MUST be treated as "campaign-bound" and blocked from client-initiated Delete or ToggleFavorite operations. Implementations SHOULD NOT rely on an additional Campaign status lookup; the presence of a CampaignId alone is sufficient to enforce protection.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No high-severity UI regressions remain in core flows (login, setup, inbox, dashboard) as verified by a 10-item UI smoke test checklist executed manually and automatically.
- **SC-002**: 0 occurrences of raw exception details shown to users in tested flows; all errors surfaced are user-friendly messages.
- **SC-003**: Projection/read-model count mismatches reduced to zero for the tested scenarios (sample dataset of 100 items) compared to current baseline.
- **SC-004**: Automated tests (unit/integration) covering fixed areas are added and pass in CI with at least 90% success rate for the new tests.

