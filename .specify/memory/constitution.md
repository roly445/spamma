<!-- Sync Impact Report

Version: 1.0.0 (initial ratification)
Templates updated: .specify/templates/plan-template.md (Constitution Check added)
-->

# Spamma Constitution

This brief constitution states the project's high-level principles and amendment process. The
principles are: developer-first self-hosting, observability and auditability, security and privacy,
testability and automation, and simplicity/modularity. Operational runbooks and specification
templates contain the detailed controls that implement these principles.

Mandatory technical constraints include a .NET 9 backend with Marten/Postgres, MediatR for CQRS,
CAP with Redis for integration events, Blazor WebAssembly frontend, and Docker Compose for local
development. The project uses StyleCop conventions, xUnit for tests with Moq, and verification
helpers; public API changes must follow semantic versioning and include migration guidance.

Changes to behavior must be delivered with tests and a CI green build. Security or infrastructure
changes require additional review and two maintainers' approvals. Amendments to this document must
be proposed via a documented PR (include rationale, version-bump classification, migration steps,
and tests), receive the required approvals, pass CI, and update the Sync Impact Report with the
merge date.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01
Sync Impact Report

Version: 1.0.0 (initial ratification)
Templates updated: .specify/templates/plan-template.md (Constitution Check added)
-->

# Spamma Constitution

This document establishes high-level principles for the Spamma project: developer-first self-hosting,
observability & auditability, security & privacy, testability & automation, and simplicity/modularity.

Key constraints: .NET 9 backend, Marten/Postgres event store, MediatR (CQRS), CAP+Redis for integration,
Blazor WebAssembly frontend, Docker Compose for local/dev, StyleCop conventions and xUnit-based testing.

PRs that change behavior MUST include tests and pass CI; security or infra changes require two approvers.

Amendments require a documented PR, maintainer approvals (1 for PATCH/MINOR, 2 for MAJOR), green CI,
and an updated Sync Impact Report in this file.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01
<!--
Sync Impact Report

Version: 1.0.0 (initial ratification)
Modified principles: Developer-first Self-Hosting; Observability & Auditability; Security & Privacy; Testability & Automation; Simplicity, Modularity & Compatibility
Templates updated: .specify/templates/plan-template.md (Constitution Check added)
Follow-ups: review spec, tasks and checklist templates for Security & Privacy alignment
-->

# Spamma Constitution

## Core Principles

### Developer-first Self-Hosting

Spamma MUST be self-hostable and runnable via Docker Compose for development. Deployments SHOULD
support production-grade hosting with TLS and configurable certificates.

Rationale: provide repeatable environments for developers, QA, and CI.

### Observability & Auditability

Spamma MUST provide structured logging, request/operation tracing where feasible, and an auditable
event trail for domain-changing operations (event-sourced via Marten). Message metadata and access logs
MUST be accessible via the UI and API.

Rationale: debugging and auditing access to potentially sensitive message content.

### Security & Privacy

Treat captured messages as potentially sensitive. Enforce RBAC, secure transport (STARTTLS/TLS for SMTP
and HTTPS for the UI), and require replacement of default development secrets before production.

Rationale: prevent accidental exposure of sensitive data.

### Testability & Automation

Provide deterministic fixtures (injectable TimeProvider), stable client contracts, and support for
programmatic cleanup and assertions. Automated unit and integration tests MUST cover ingestion and core
domain logic.

Rationale: integration into automated test pipelines is essential for adoption.

### Simplicity, Modularity & Compatibility

Follow a modular-monolith pattern with clear module boundaries (UserManagement, DomainManagement,
EmailInbox, Common). Public API changes MUST include versioning and migration guidance.

Rationale: reduce cognitive load and simplify upgrades.

## Constraints & Standards

- Backend: .NET 9, Marten (Postgres), MediatR, FluentValidation.
- Messaging: CAP with Redis.
- Frontend: Blazor WebAssembly, Tailwind CSS (webpack), TypeScript assets compiled to wwwroot.
- Infrastructure: Docker Compose for local/dev (Postgres, Redis, MailHog); production TLS via .pfx or Let's Encrypt.
- Code quality: one public type per file; follow StyleCop and shared GlobalSuppressions.cs exceptions.
- Testing: xUnit + Moq (Strict) + verification helpers; use injected TimeProvider.

## Development & Release Gates

- Work on feature branches and include tests for new behavior.
- PRs must pass CI: build, lint, unit tests, and module verification.
- Security or infrastructure changes require two approvers and a migration plan.

Constitution Check (PR template items): unit + integration tests; observability plan; security/privacy review; static analysis; frontend build verification; API migration guidance if applicable.

## Governance

Amendments require a documented PR, maintainer approvals (1 for PATCH/MINOR, 2 for MAJOR), green CI, and an updated Sync Impact Report in this file.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01

<!--
Sync Impact Report

Version: 1.0.0 (initial ratification)
Templates updated: .specify/templates/plan-template.md (Constitution Check added)
Follow-ups: review spec, tasks and checklist templates for Security & Privacy alignment
-->

# Spamma Constitution

This constitution is the project's high-level governance document. It states the primary principles
that guide design, security, and release decisions for Spamma. It is intentionally concise; operational
runbooks and specification templates implement these principles in detail.

Principles (summary):
- Developer-first self-hosting: runnable via Docker Compose for development; production-capable hosting
	with TLS and configurable certificates.
- Observability & auditability: structured logs, tracing where feasible, and auditable event trails for
	domain changes (event-sourced via Marten). Message metadata and access logs exposed via UI/API.
- Security & privacy: treat captured messages as sensitive; enforce RBAC, secure transport (STARTTLS/TLS,
	HTTPS), and require replacement of default development secrets before production.
- Testability & automation: deterministic fixtures (injectable TimeProvider), stable client contracts,
	and comprehensive unit/integration tests for ingestion and domain logic.
- Simplicity, modularity & compatibility: modular-monolith with clear module boundaries; API changes
	must include versioning and migration guidance.

Mandatory constraints and standards:
- Backend: .NET 9, Marten (Postgres), MediatR (CQRS), FluentValidation.
- Messaging/integration: CAP with Redis.
- Frontend: Blazor WebAssembly, Tailwind CSS (webpack), TypeScript assets compiled to wwwroot.
- Infrastructure: Docker Compose for local/dev (Postgres, Redis, MailHog); production TLS via .pfx or Let's Encrypt.
- Code quality: one public type per file; conform to StyleCop with documented suppressions in shared/GlobalSuppressions.cs.
- Testing: xUnit + Moq (Strict) with verification helpers; use injected TimeProvider.

Development and release gates (must be enforced by PR templates and CI):
- Feature branch with tests for new behavior.
- CI green: build, lint, unit tests, module verification.
- Observability & audit plan for changes that affect message handling or storage.
- Security & privacy review for changes touching message access, storage, authentication, or transport.
- Frontend assets must build successfully in CI.

Governance / Amendments:
1) Propose amendment via PR that includes proposed text, rationale, version-bump classification (MAJOR/MINOR/PATCH), migration steps, and tests.
2) Approvals: 1 maintainer for PATCH/MINOR; 2 maintainers for MAJOR.
3) CI must pass; merge after approvals.
4) Update this file's Sync Impact Report and set LAST_AMENDED_DATE to the merge date.

Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01
MUST be runnable via Docker Compose for local and development environments and support production-grade
hosting (TLS, configurable certificates, and environment configuration) for staging and production use.

Rationale: Developers need a trustworthy, repeatable environment for email testing that mirrors
production as closely as practical.

### II. Observability & Auditability

Spamma MUST provide structured logging, request/operation tracing where feasible, and an auditable
event trail for all domain-changing operations (event-sourced via Marten). The system MUST surface
message metadata, headers, body, and attachment access logs in the UI and via API.

Rationale: Debugging email flows and auditing access to potentially sensitive message content are
primary use cases.

### III. Security & Privacy

Spamma MUST treat captured messages as potentially sensitive data. Access controls (multi-user accounts
and role-based permissions) MUST protect message access. Transport security (STARTTLS/TLS on SMTP and
HTTPS for the UI) MUST be supported. Default development credentials and setup secrets MUST be replaced
before production use.

Rationale: Even development fixtures can contain sensitive data; preventing accidental exposure is
mandatory.

### IV. Testability & Automation

Spamma MUST be automatable and observable from CI: provide a stable REST API (Swagger where applicable),
deterministic test fixtures (injectable TimeProvider), and support for programmatic cleanup and
assertions. Automated tests (unit and integration) MUST cover domain logic and message ingestion paths.

Rationale: The product is primarily useful when it integrates into automated test pipelines.

### V. Simplicity, Modularity & Compatibility

Design MUST favour a modular-monolith pattern: clear, well-separated modules (UserManagement,
DomainManagement, EmailInbox, Common) with well-defined contracts. Public APIs and client contracts used
by Blazor WebAssembly MUST be stable; breaking changes require explicit versioning and migration guidance.
Keep the default UX and APIs simple and predictable.

Rationale: Lowers cognitive load for adopters and makes the system easier to operate.

## Constraints & Standards

Spamma follows these mandatory stack and style constraints:

- Backend: .NET 9, Marten (Postgres) for event sourcing, MediatR for CQRS, FluentValidation for validation.
- Messaging/integration: CAP with Redis for cross-module integration events.
- Frontend: Blazor WebAssembly, Tailwind CSS (via webpack) and TypeScript assets compiled to wwwroot.
- Infrastructure: Docker Compose for local/dev stacks (Postgres, Redis, MailHog), with production-capable
  TLS handling (user-provided .pfx or Let's Encrypt automation from setup wizard).
- Code quality: enforce "one public type per file" and StyleCop conventions (see shared/stylecop.json and
  GlobalSuppressions.cs for intentional suppressions).
- Testing: xUnit + Moq (Strict) + verification helpers. Use injected TimeProvider for deterministic tests.

## Development Workflow & Quality Gates

- All changes MUST be delivered on a feature branch and include tests covering new behavior.
- Pull requests MUST pass the CI pipeline: build, lint, unit tests, and any module-specific verification tests.
- Code review: at least one approving maintainer required; for security or infrastructure changes two
  maintainer approvals are REQUIRED and a short migration plan must be included.
- Release and versioning: follow semantic versioning for public APIs. Governance changes follow the
  Governance amendment process below.

Constitution Check (to be included in PR templates and planning docs):

- Unit and integration tests added or updated for behavior changes.
- Observability & audit plan included for changes that affect message handling or storage.
- Security & privacy review included for changes touching message access, storage, authentication, or transport.
- Code style and static analysis checks pass (StyleCop, Roslyn analyzers).
- Frontend assets build successfully in CI (npm build or equivalent), and public API contract changes
  include migration guidance.

## Governance

Amendments to this constitution MUST follow these steps:

1. File a documented PR that includes:
   - The proposed amendment text and rationale.
   - A classification of the version bump (MAJOR, MINOR, PATCH) with reasoning.
   - Any migration steps and tests required to remain compliant.

2. Obtain approvals: at least one maintainer approval for PATCH/MINOR; two maintainers for MAJOR governance changes.

3. CI checks MUST pass. Merge only after approvals and a green CI.

4. Update this constitution file with an updated Sync Impact Report and set LAST_AMENDED_DATE to the date of merge.

Versioning policy summary:

- MAJOR: Backward-incompatible governance or principle redefinitions or removals.
- MINOR: Addition of new principle/section or material expansions of guidance.
- PATCH: Clarifications, wording fixes, or non-semantic refinements.

**Version**: 1.0.0 | **Ratified**: 2025-11-01 | **Last Amended**: 2025-11-01

Notes:

- This constitution is intentionally concise. Operational runbooks, security runbooks, and the
  specification templates contain the detailed operational and security controls that implement
  these principles. Changes to those artifacts should reference this constitution in their PR descriptions.
<!--
<!-- Sync Impact Report

Version: 1.0.0 (initial ratification)
Modified principles: Developer-first Self-Hosting; Observability & Auditability; Security & Privacy; Testability & Automation; Simplicity, Modularity & Compatibility
Templates updated: .specify/templates/plan-template.md (Constitution Check added)
Follow-ups: review spec, tasks and checklist templates for Security & Privacy alignment
-->

# Spamma Constitution

## Core Principles

### I. Developer-first Self-Hosting

Spamma MUST be a private, self-hostable email-capture service that enables developers, QA, and CI environments to capture and inspect SMTP traffic without relying on third-party services. Deployments MUST be runnable via Docker Compose for development and support production-capable hosting (TLS, configurable certificates, and environment configuration).

Rationale: Developers need a repeatable environment for email testing that mirrors production where practical.

### II. Observability & Auditability

Spamma MUST provide structured logs, request/operation tracing where feasible, and an auditable event trail for domain-changing operations (event-sourced via Marten). The system MUST surface message metadata, headers, body, and attachment access logs in the UI and via API.

Rationale: Debugging email flows and auditing access to message content are primary use cases.

### III. Security & Privacy

Captured messages are potentially sensitive. Spamma MUST enforce access controls (multi-user accounts and role-based permissions) and protect transport (STARTTLS/TLS for SMTP, HTTPS for the UI). Default development credentials and setup secrets MUST be replaced before production use.

Rationale: Prevent accidental exposure of sensitive data, even in development contexts.

### II. Observability & Auditability
Spamma MUST provide structured logging, request/operation tracing (where feasible), and an auditable
event trail for all domain-changing operations (event-sourced via Marten). The system MUST surface
message metadata, headers, body, and attachment access logs in the UI and via API. Rationale: debugging
email flows and auditing access to potentially sensitive message content are primary use cases.

### III. Security & Privacy
Spamma MUST treat captured messages as potentially sensitive data. Access controls (multi-user accounts
and role-based permissions) MUST protect message access. Transport security (STARTTLS/TLS on SMTP,
HTTPS for the UI) MUST be supported. Default dev credentials and setup secrets MUST be replaced before
production use. Rationale: even development fixtures can contain sensitive data; preventing accidental
exposure is mandatory.

### IV. Testability & Automation
Spamma MUST be automatable and observable from CI: provide a stable REST API (Swagger), deterministic
test fixtures (injectable TimeProvider), and support for programmatic cleanup and assertions. Tests
(unit and integration) MUST be included for domain logic and message ingestion. Rationale: the product is
primarily useful when it integrates into automated test pipelines.

### V. Simplicity, Modularity & Compatibility
Design MUST favour a modular-monolith pattern: clear, well-separated modules (UserManagement,
DomainManagement, EmailInbox, Common) with well-defined contracts. Public APIs and client contracts used
by Blazor WASM MUST be stable; breaking changes require explicit versioning and migration guidance. Keep
the default UX and APIs simple and predictable. Rationale: lowers cognitive load for adopters and makes
the system easier to operate.

## Constraints & Standards

Spamma follows these mandatory stack and style constraints:
- Backend: .NET 9, Marten (Postgres) for event sourcing, MediatR for CQRS, FluentValidation for validation.
- Messaging/integration: CAP with Redis for cross-module integration events.
- Frontend: Blazor WebAssembly, Tailwind CSS (via webpack) and TypeScript assets compiled to wwwroot.
- Infrastructure: Docker Compose for local/dev stacks (Postgres, Redis, MailHog), with production-capable TLS
	handling (user-provided .pfx or Let's Encrypt automation from setup wizard).
- Code quality: enforce "one public type per file" and StyleCop conventions (see shared/stylecop.json and
	GlobalSuppressions.cs for intentional suppressions).
- Testing: xUnit + Moq (Strict) + verification helpers. Use injected TimeProvider for deterministic tests.

## Development Workflow & Quality Gates

- All changes MUST be delivered on a branch per feature and include tests covering new behavior.
- Pull requests MUST pass the CI pipeline: build, lint, unit tests, and any module-specific verification tests.
- Code review: at least one approving maintainer required; for security or infra changes, two maintainers'
	approval is REQUIRED and a short migration plan included.
- Release and versioning: follow semantic versioning for public APIs; changes to governance or principles
	follow the Governance amendment process below.

## Governance

Amendments to this constitution MUST follow these steps:
1. File a documented PR that includes:
	 - The proposed amendment text and rationale.
	 - A classification of the version bump (MAJOR, MINOR, PATCH) with reasoning.
	 - Any migration steps and tests required to remain compliant.
2. Obtain approvals: at least one maintainer approval for PATCH/MINOR, two maintainers for MAJOR governance
	<!-- Sync Impact Report

	Version: 1.0.0 (initial ratification)
	Templates updated: .specify/templates/plan-template.md (Constitution Check added)
	-->

	# Spamma Constitution

	This document records the project's high-level principles and the amendment process. It is
	intentionally short; operational runbooks and specification templates implement these principles
	in detail.

	Principles:

	- Developer-first self-hosting: runnable via Docker Compose for development; support production-grade
		hosting with TLS and configurable certificates.

	- Observability & auditability: structured logs, tracing where feasible, and auditable event trails
		(event-sourced via Marten). Message metadata and access logs must be available via UI and API.

	- Security & privacy: treat captured messages as sensitive; enforce RBAC and secure transport
		(STARTTLS/TLS, HTTPS). Replace default development secrets before production use.

	- Testability & automation: provide deterministic fixtures (injectable TimeProvider), stable client
		contracts, and automated unit/integration tests covering ingestion and domain logic.

	- Simplicity, modularity & compatibility: modular-monolith pattern; public API changes must include
		versioning and migration guidance.

	Mandatory constraints:

	- Backend: .NET 9, Marten/Postgres, MediatR (CQRS), FluentValidation.
	- Messaging/integration: CAP with Redis.
	- Frontend: Blazor WebAssembly, Tailwind CSS (webpack), TypeScript assets compiled to wwwroot.
	- Infra: Docker Compose for local/dev (Postgres, Redis, MailHog); production TLS via .pfx or Let's Encrypt.
	- Code quality: one public type per file; follow StyleCop conventions with documented suppressions.
	- Testing: xUnit + Moq (Strict) with verification helpers; use injected TimeProvider.

	Amendments and governance:

	1. Propose amendment via PR including proposed text, rationale, version-bump classification (MAJOR/MINOR/PATCH),
		 migration steps, and tests.
	2. Obtain approvals: 1 maintainer for PATCH/MINOR; 2 maintainers for MAJOR.
	3. CI must pass; merge after approvals.
	4. Update this file's Sync Impact Report and set LAST_AMENDED_DATE to the merge date.

	Version: 1.0.0 | Ratified: 2025-11-01 | Last Amended: 2025-11-01
- Testing: xUnit + Moq (Strict) + verification helpers. Use injected TimeProvider for deterministic tests.

## Development Workflow & Quality Gates

- All changes MUST be delivered on a branch per feature and include tests covering new behavior.
- Pull requests MUST pass the CI pipeline: build, lint, unit tests, and any module-specific verification tests.
- Code review: at least one approving maintainer required; for security or infra changes, two maintainers' approval
	is REQUIRED and a short migration plan included.
- Release and versioning: follow semantic versioning for public APIs; changes to governance or principles follow
	the Governance amendment process below.

## Governance

Amendments to this constitution MUST follow these steps:
1. File a documented PR that includes:
	 - The proposed amendment text and rationale.
	 - A classification of the version bump (MAJOR, MINOR, PATCH) with reasoning.
	 - Any migration steps and tests required to remain compliant.
2. Obtain approvals: at least one maintainer approval for PATCH/MINOR, two maintainers for MAJOR governance changes.
3. CI checks MUST pass. Merge only after approvals and a green CI.
4. Update the constitution file at `.specify/memory/constitution.md` with an updated Sync Impact Report and
	 set LAST_AMENDED_DATE to the date of merge.

Versioning policy summary:
- MAJOR: Backward-incompatible governance or principle redefinitions or removals.
- MINOR: Addition of new principle/section or material expansions of guidance.
- PATCH: Clarifications, wording fixes, or non-semantic refinements.

**Version**: 1.0.0 | **Ratified**: 2025-11-01 | **Last Amended**: 2025-11-01
Design MUST favour a modular-monolith pattern: clear, well-separated modules (UserManagement, DomainManagement,
EmailInbox, Common) with well-defined contracts. Public APIs and client contracts used by Blazor WASM MUST be
stable; breaking changes require explicit versioning and migration guidance. Keep the default UX and APIs
simple and predictable. Rationale: lowers cognitive load for adopters and makes the system easier to operate.

## Constraints & Standards

Spamma follows these mandatory stack and style constraints:
- Backend: .NET 9, Marten (Postgres) for event sourcing, MediatR for CQRS, FluentValidation for validation.
- Messaging/integration: CAP with Redis for cross-module integration events.
- Frontend: Blazor WebAssembly, Tailwind CSS (via webpack) and TypeScript assets compiled to wwwroot.
- Infrastructure: Docker Compose for local/dev stacks (Postgres, Redis, MailHog), with production-capable TLS
	handling (user-provided .pfx or Let's Encrypt automation from setup wizard).
- Code quality: enforce "one public type per file" and StyleCop conventions (see shared/stylecop.json and
	GlobalSuppressions.cs for intentional suppressions).
- Testing: xUnit + Moq (Strict) + verification helpers. Use injected TimeProvider for deterministic tests.

## Development Workflow & Quality Gates

- All changes MUST be delivered on a branch per feature and include tests covering new behavior.
- Pull requests MUST pass the CI pipeline: build, lint, unit tests, and any module-specific verification tests.
- Code review: at least one approving maintainer required; for security or infra changes, two maintainers' approval
	is REQUIRED and a short migration plan included.
- Release and versioning: follow semantic versioning for public APIs; changes to governance or principles follow
	the Governance amendment process below.

## Governance

Amendments to this constitution MUST follow these steps:
1. File a documented PR that includes:
	 - The proposed amendment text and rationale.
	 - A classification of the version bump (MAJOR, MINOR, PATCH) with reasoning.
	 - Any migration steps and tests required to remain compliant.
2. Obtain approvals: at least one maintainer approval for PATCH/MINOR, two maintainers for MAJOR governance changes.
3. CI checks MUST pass. Merge only after approvals and a green CI.
4. Update the constitution file at `.specify/memory/constitution.md` with an updated Sync Impact Report and
	 set LAST_AMENDED_DATE to the date of merge.

Versioning policy summary:
- MAJOR: Backward-incompatible governance or principle redefinitions or removals.
- MINOR: Addition of new principle/section or material expansions of guidance.
- PATCH: Clarifications, wording fixes, or non-semantic refinements.

**Version**: 1.0.0 | **Ratified**: 2025-11-01 | **Last Amended**: 2025-11-01

