# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Protect campaign-bound emails from client-initiated deletion and favouriting while keeping server authoritative enforcement. Implementation will:

- Add server-side checks in Email command handlers to return a failed `CommandResult` with `BluQubeErrorData` (error code `EmailIsPartOfCampaign`) when an Email has a non-null `CampaignId`.
- Update client UI (`EmailViewer`) to hide Delete and Favorite controls for campaign-bound emails.
- Add unit tests for handlers and API-level contract tests verifying BluQube error semantics.
- No admin/force override; deleting a Campaign synchronously cascades removal of the single associated email (campaign:email is 1:1). Audit logging for domain-changing actions (including cascade deletions) IS REQUIRED and MUST emit auditable events (for example, Marten events or dedicated audit records) that record the initiating actor and timestamp. Include tests that validate these audit events are emitted when cascade deletes occur.

This plan creates small, focused PRs: (1) handler + tests, (2) client UI, (3) integration/contract tests.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: .NET 9 (C#)  
**Primary Dependencies**: Marten (Postgres event store), MediatR, FluentValidation, CAP (Redis), Blazor WebAssembly, xUnit/Moq for tests.  
**Storage**: PostgreSQL (Marten event store + read-model projections).  
**Testing**: xUnit for unit tests, integration tests use test fixtures and in-memory or test DB, Contract tests assert on BluQubeErrorData.  
**Target Platform**: Linux/Windows server for backend, Blazor WASM for frontend.  
**Project Type**: Web application (backend services + Blazor WebAssembly frontend).  
**Performance Goals**: No new global perf targets for this small sweep; ensure handler checks are O(1) and do not add heavy lookups.  
**Constraints**: Must follow repo constitution (zero new compiler warnings, one public type per file, Blazor split).  
**Scale/Scope**: Small-scope changes limited to EmailInbox module and frontend EmailViewer component.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The following checks MUST be validated and documented in the plan before research proceeds:

- Tests: Unit tests for new handler logic and at least one API-level contract test asserting `BluQubeErrorData` and `EmailIsPartOfCampaign` code. (PASS — tests will be added as part of PRs.)
- Observability: No new observability requirements for this change; use existing structured logging. (PASS — no added obligations; ensure handler errors are logged as existing patterns.)
- Security & Privacy: Respect existing RBAC and secure transports; no change to retention. (PASS — no new secrets or transports.)
- Code Quality: Must compile without warnings and follow StyleCop/conventions. (PASS — enforced in PR checks.)
- CI Compatibility: Ensure tests run in CI; frontend build should be exercised for the UI change. (PASS — small frontend change only.)

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: Web application - update EmailInbox server handlers and EmailViewer client component. Relevant paths:

- Server handlers: `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Email/`  
- Client component: `src/Spamma.App/Spamma.App.Client/Components/UserControls/EmailViewer.razor` and `.razor.cs`  
- Tests: `tests/Spamma.Modules.EmailInbox.Tests/` (handler tests) and `tests/Spamma.App.Tests/` or integration test project for API contract tests.

Deliverables (created by this plan): `specs/001-bug-sweep/research.md`, `specs/001-bug-sweep/data-model.md`, `specs/001-bug-sweep/quickstart.md`, `specs/001-bug-sweep/contracts/openapi-email.yaml`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
