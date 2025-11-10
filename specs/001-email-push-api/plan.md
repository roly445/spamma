# Implementation Plan: Developer First Push API

**Branch**: `001-email-push-api` | **Date**: November 10, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-email-push-api/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Real-time push API for email notifications using gRPC, allowing developers to subscribe to email events in their subdomains with JWT authentication and minimal payload (to, from, subject, ID), plus full EML retrieval.

## Technical Context

**Language/Version**: .NET 9 (C#)  
**Primary Dependencies**: Marten (event store), MediatR (CQRS), FluentValidation, gRPC.AspNetCore  
**Storage**: PostgreSQL (via Marten event store)  
**Testing**: xUnit with custom verification helpers  
**Target Platform**: .NET 9 server with Blazor WebAssembly client  
**Project Type**: Modular monolith with Blazor WASM frontend  
**Performance Goals**: Push notifications delivered within 1 second of email receipt for 99% of messages  
**Constraints**: JWT authentication, subdomain viewer permissions, gRPC streaming  
**Scale/Scope**: Unlimited integrations per user and emails per subdomain (operational limits apply)  
**API Generation**: REST APIs auto-generated from BluQube attributes on CQRS commands/queries; no manual controllers required

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The following checks MUST be validated and documented in the plan before research proceeds:

- **Tests**: Unit tests for new domain logic (push integration management) and integration tests for gRPC streaming and email push notifications. Verification-based testing with custom helpers for domain aggregates.
- **Observability**: No logging required for push notifications (infrastructure-level monitoring); event tracing via Marten for integration creation/updates.
- **Security & Privacy**: JWT authentication for all API access; subdomain viewer permissions enforced; gRPC over HTTPS; no sensitive data in push payloads beyond email metadata.
- **Code Quality**: Conformance with repository conventions (one public type per file, StyleCop rules). No XML documentation comments for intent. Zero compiler warnings. Blazor components split into `.razor` + `.razor.cs`. Commands/Queries in `.Client` projects, handlers in server projects. No new projects added.
- **CI Compatibility**: Feature builds and tests in CI; gRPC services testable in headless environment.

**GATE STATUS**: PASSED - All checks validated and no violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/001-email-push-api/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/modules/
├── Spamma.Modules.EmailInbox/
│   ├── Application/
│   │   ├── CommandHandlers/
│   │   │   └── PushIntegration/  # NEW: CreatePushIntegrationCommandHandler, UpdatePushIntegrationCommandHandler, DeletePushIntegrationCommandHandler
│   │   ├── QueryProcessors/
│   │   │   └── PushIntegration/  # NEW: GetPushIntegrationsQueryProcessor
│   │   ├── Validators/
│   │   │   └── PushIntegration/  # NEW: CreatePushIntegrationCommandValidator, etc.
│   │   └── Authorizers/
│   │       └── PushIntegration/  # NEW: PushIntegrationAuthorizer
│   ├── Domain/
│   │   └── PushIntegrationAggregate/  # NEW: PushIntegration aggregate with events
│   ├── Infrastructure/
│   │   ├── Projections/
│   │   │   └── PushIntegrationProjection/  # NEW: Marten projection for push integrations
│   │   ├── Services/
│   │   │   └── PushNotificationService/  # NEW: gRPC service implementation
│   │   └── Repositories/
│   │       └── PushIntegrationRepository/  # NEW: Repository for push integrations
│   └── EmailInbox.csproj
├── Spamma.Modules.EmailInbox.Client/
│   ├── Application/
│   │   ├── Commands/
│   │   │   └── PushIntegration/  # NEW: CreatePushIntegrationCommand, UpdatePushIntegrationCommand, DeletePushIntegrationCommand
│   │   └── Queries/
│   │       └── PushIntegration/  # NEW: GetPushIntegrationsQuery
│   └── EmailInbox.Client.csproj
├── Spamma.Modules.Common/
│   ├── IntegrationEvents/
│   │   └── PushIntegrationCreatedIntegrationEvent/  # NEW: Event for cross-module notifications
│   └── Common.csproj
└── Spamma.App/
    ├── Spamma.App/
    │   ├── Services/
    │   │   └── PushNotificationGrpcService/  # NEW: gRPC service registration
    │   └── Program.cs  # Update: Register gRPC service
    └── Spamma.App.Client/  # No changes for push API (server-side only)
```

**Structure Decision**: Document the selected structure and reference the real directories captured above

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
