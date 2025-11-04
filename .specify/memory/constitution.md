<!-- Canonical Spamma Constitution -->
<!-- Canonical Spamma Constitution (generated) -->

# Spamma Constitution

<!--
Sync Impact Report:
- Version change: 1.0.0 -> 1.1.0
- Modified principles: Code Quality & Project Structure (clarified no XML comments; zero compiler warnings; Blazor split requirement)
- Added rules: "No new projects without approval"; "Commands/Queries in .Client; Handlers in server"; "Blazor razor/codebehind split"
- Added sections: Code Quality explicit rules
- Templates updated: .specify/templates/spec-template.md ✅ updated, .specify/templates/plan-template.md ✅ updated
- Follow-up TODOs: review tasks-template.md for task categorization ⚠ pending (file: .specify/templates/tasks-template.md)
-->

This constitution records the project's high-level principles and the amendment process. It is
intentionally concise; operational runbooks and specification templates implement these
principles in detail.

Version: 1.1.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-04

## Core Principles

1. Developer-first Self-Hosting

   Spamma MUST be self-hostable and runnable via Docker Compose for development. Production
   deployments SHOULD support TLS and configurable certificates.

2. Observability & Auditability

   Provide structured logging, request/operation tracing where feasible, and an auditable
   event trail for domain-changing operations (event-sourced via Marten). Message metadata and
   access logs MUST be exposed via UI and API where appropriate.

3. Security & Privacy

   Treat captured messages as potentially sensitive. Enforce RBAC and secure transport (STARTTLS/TLS
   for SMTP and HTTPS for the UI). Replace default development secrets before production.

4. Testability & Automation

   Provide deterministic fixtures (injectable TimeProvider), stable client contracts, and support
   programmatic cleanup and assertions. Automated unit and integration tests MUST cover ingestion
   and core domain logic.

5. Simplicity, Modularity & Compatibility

   Follow a modular-monolith pattern with clear module boundaries (UserManagement, DomainManagement,
   EmailInbox, Common). Public API changes MUST include versioning and migration guidance.

## Client vs Non-Client project rules

Modules follow a dual-project pattern: a client project (suffix `.Client`) that exposes contracts
and DTOs for the Blazor WASM frontend and other modules, and a server project that contains the
implementations and domain logic.

Client projects (for example `Spamma.Modules.DomainManagement.Client`):

- Contain only serializable contracts, DTOs and small primitives intended for client-side use.
- Host Commands and CommandResult/QueryResult types, simple DTOs and enums used by the UI (for example `SmtpResponseCode`).
- MUST NOT contain domain business logic or server-side implementations.

Server projects (non-`.Client`):

- Contain domain aggregates, events and business logic, command handlers, query processors,
  repositories, projections and other infrastructure.
- Implement the handlers and processors for contracts defined in the corresponding `.Client` project.

Rationale: Keep client projects minimal and serializable; place shared client-facing enums/DTOs
in `Spamma.Modules.Common.Client` to avoid duplicate-type conflicts.

 - Folder organization by aggregate
    - Commands, command results and query/response DTOs in `.Client` projects SHOULD be
       grouped by aggregate using a folder-per-aggregate structure, e.g. `Application/Commands/Domain/*`,
       `Application/Commands/Subdomain/*`, `Application/Commands/ChaosAddress/*`.
       Small, truly cross-cutting DTOs may remain in `Spamma.Modules.Common.Client`.
    - Server projects MUST mirror the same aggregate grouping for handlers, validators,
       authorizers and related logic, e.g. `Application/CommandHandlers/Domain/*`,
       `Application/Validators/Domain/*`, `Application/Authorizers/Commands/Domain/*`.
    - Rationale: colocating contract, handler, validation and authorization code by aggregate
       reduces duplication, improves discoverability, and simplifies reviews and maintenance.

## Development & Release Gates

- Work on feature branches and include tests for new behavior.
- PRs must pass CI: build, lint, unit tests, and module verification.
- Security or infrastructure changes require two approvers and a migration plan.

Constitution Check (PR checklist): unit + integration tests; observability plan; security/privacy review; static analysis; frontend build verification; API migration guidance if applicable.

## Governance and Amendments

Amendments require a documented PR that includes the proposed text, rationale, version-bump
classification (MAJOR/MINOR/PATCH), migration steps and tests. Approvals: 1 maintainer for
PATCH/MINOR; 2 maintainers for MAJOR. CI must pass. Update this file's Sync Impact Report and set
Last Amended to the merge date.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01
