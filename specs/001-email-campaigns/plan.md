# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]
**Project Type**: [single/web/mobile - determines source structure]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

**Language/Version**: .NET 9 (C#) - matches repository baseline.
**Primary Dependencies**: Marten (PostgreSQL event store & projections), MediatR/CQRS (BluQube patterns), Blazor WebAssembly (frontend), Tailwind CSS, ApexCharts (frontend charting), CAP + Redis for integration events.
**Storage**: PostgreSQL (Marten event store + projections), local file store or object store for optional sample content provider implementations.
**Testing**: xUnit + Moq for unit/integration tests (matches repo test conventions).
**Target Platform**: Web application - server (.NET) + Blazor WASM client.
**Project Type**: Web (frontend + backend modular monolith).
**Performance Goals**: Meet SC-001..SC-005 from spec (e.g., campaign list page renders first page ≤1.5s for up to 1000 campaigns; 95% of captures tracked within 5s under normal load).
**Constraints**: Conform to repository constitution (StyleCop rules, one public type per file, Blazor split `.razor` + `.razor.cs`), preserve zero compiler warnings, do not add new csproj without approval.
**Scale/Scope**: Designed for subdomain-level usage; expected to handle bursts of thousands of messages per minute per subdomain with projection aggregation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The following checks MUST be validated and documented in the plan before research proceeds:

- Tests: Unit tests for new domain logic and at least one integration test for message ingestion where applicable.
- Observability: Logging and/event tracing plan for the feature (how message metadata and access will be logged).
- Security & Privacy: Any access control, transport security (TLS), and data retention implications must be
  described and approved.
- Code Quality: Conformance with repository conventions (one public type per file, StyleCop rules as configured).
  - Additional Code Quality GATES: No XML documentation comments for intent (use clear naming), zero compiler warnings, and Blazor components split into `.razor` + `.razor.cs` for interactive UI.
  - Commands/Queries placement: Verify Commands and Queries exist in the `.Client` project and that server-side handlers/validators are in the non-`.Client` project.
  - Project additions: Any new project addition must be justified in the plan and approved before creation.
- CI Compatibility: The feature must be runnable in CI (headless build of frontend assets if applicable) and pass
  the project's automated checks locally before the PR is opened.

Constitution check results (preliminary):
- Tests: PASS (plan includes unit + integration test tasks in Phase 2).
- Observability: PARTIAL (research.md lists audit/receipt logging; implementation tasks will add metrics and traces).
- Security & Privacy: PASS (spec explicitly prohibits persisting full message bodies by default; sample retention and export controls included).
- Code Quality: PASS (plan will adhere to repo conventions; no new projects planned unless justified).
- CI Compatibility: PARTIAL (frontend asset build must be verified in CI; Phase 2 tasks will add headless build test asserts).

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

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

**Structure Decision**: Option 2 - Web application. Use existing modular monolith layout:
- Backend: `src/modules/Spamma.Modules.EmailInbox` and `src/modules/Spamma.Modules.DomainManagement` for capture handlers, projections, and query processors.
- Frontend client: `src/Spamma.App/Spamma.App.Client` (Blazor WASM) for Campaigns pages and ApexCharts integration.

Rationale: Matches repository conventions and existing module boundaries. No new csproj will be added; artifacts will be implemented within existing modules and client project.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
