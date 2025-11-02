<!-- Canonical Spamma Constitution (generated) -->

<!-- Canonical Spamma Constitution backup -->

# Spamma Constitution (canonical backup)

This is a backup of the canonical Spamma Constitution kept for reference.

<!--
Sync Impact Report

Version: 1.0.0 (initial ratification)
Templates updated: .specify/templates/plan-template.md (Constitution Check added)
-->

# Spamma Constitution

This constitution records the project's high-level principles and the amendment process. It is
intentionally concise; operational runbooks and specification templates implement these
principles in detail.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01

## Core Principles

1. Developer-first Self-Hosting

   Spamma MUST be self-hostable and runnable via Docker Compose for development. Production
   deployments SHOULD support TLS and configurable certificates.

2. Observability & Auditability

   Provide structured logging, request/operation tracing where feasible, and an auditable
   event trail for domain-changing operations (event-sourced via Marten). Message metadata and
   access logs MUST be exposed via UI and API where appropriate.

3. Security & Privacy

   Treat captured messages as potentially sensitive. Enforce RBAC, secure transport (STARTTLS/TLS
   for SMTP and HTTPS for the UI), and require replacement of default development secrets before
   production.

4. Testability & Automation

   Provide deterministic fixtures (injectable TimeProvider), stable client contracts, and support
   programmatic cleanup and assertions. Automated unit and integration tests MUST cover ingestion
   and core domain logic.

5. Simplicity, Modularity & Compatibility

   Follow a modular-monolith pattern with clear module boundaries (UserManagement, DomainManagement,
   EmailInbox, Common). Public API changes MUST include versioning and migration guidance.

## Constraints & Standards

- Backend: .NET 9, Marten (Postgres), MediatR (CQRS), FluentValidation.
- Messaging: CAP with Redis for integration events.
- Frontend: Blazor WebAssembly, Tailwind CSS (webpack), TypeScript assets compiled to wwwroot.
- Infrastructure: Docker Compose for local/dev (Postgres, Redis, MailHog); production TLS via .pfx or Let's Encrypt.
- Code quality: one public type per file; follow StyleCop conventions and the project's GlobalSuppressions.cs.
- Testing: xUnit + Moq (Strict) + verification helpers; use injected TimeProvider.

## Client vs Non-Client project rules

Modules follow a dual-project pattern: a client project (suffix `.Client`) that exposes contracts
and DTOs for the Blazor WASM frontend and other modules, and a server project that contains the
implementations and domain logic.

Client projects (for example `Spamma.Modules.DomainManagement.Client`):
- Contain only serializable contracts, DTOs and small primitives intended for client-side use.
- Host Commands and CommandResult/response types, Queries and QueryResult types, simple DTOs and
  enums used by the UI (for example `SmtpResponseCode` when the client needs it), and any
  BluQube attributes or API-shaping annotations consumed by code generation or runtime wiring.
- MUST NOT contain domain business logic or server-side implementations.

Server projects (non-`.Client`):
- Contain domain aggregates, events and business logic, command handlers, query processors,
  repositories, projections and other infrastructure.
- Implement the handlers and processors for contracts defined in the `.Client` project.
- MUST NOT expose server internals to the WASM client via direct project references.

Rules and rationale:

1. If the frontend or another module needs to send a command or run a query, the command/query
   and its result DTO MUST be defined in the corresponding `.Client` project. The server project
   implements the handler/processor for that contract. This prevents accidental coupling and
   duplicate-type conflicts (CS0433/CS0234).

2. Keep client projects minimal and serializable. Avoid embedding behavior or domain logic in
   client artifacts.

3. When sharing small primitives intended for the client (enums, small DTOs), prefer placing them
   in a broadly shared client project (for example `Spamma.Modules.Common.Client`) to avoid
   duplicate-type definitions across modules.

4. Maintain the project's one-public-type-per-file rule across client and server projects to
   satisfy StyleCop and simplify audits.

5. When adding a new UI-facing API, scaffold four files in the `.Client` project: the Query/Command
   and the QueryResult/CommandResult (each in their own files). Decorate with BluQube attributes
   where applicable for WASM path generation.

Rationale: This separation prevents accidental coupling of UI code to server internals, avoids
duplicate-type and visibility problems, and keeps the generated WASM surface small and stable.

## Development & Release Gates

- Work on feature branches and include tests for new behavior.
- PRs must pass CI: build, lint, unit tests, and module verification.
- Security or infrastructure changes require two approvers and a migration plan.

Constitution Check (PR checklist): unit + integration tests; observability plan; security/privacy review;
static analysis; frontend build verification; API migration guidance if applicable.

## Governance and Amendments

Amendments require a documented PR that includes the proposed text, rationale, version-bump
classification (MAJOR/MINOR/PATCH), migration steps and tests. Approvals: 1 maintainer for
PATCH/MINOR; 2 maintainers for MAJOR. CI must pass. Update this file's Sync Impact Report and set
Last Amended to the merge date.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01
