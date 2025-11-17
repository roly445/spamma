# Implementation Plan: API Key Management

**Branch**: `001-api-key-management` | **Date**: November 10, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-api-key-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Replace JWT authentication with API key mechanism for public endpoints. Users can create, view metadata, and revoke API keys via UI. Keys are viewable once after creation and use cryptographically secure generation with HTTPS requirements.

## Technical Context

**Language/Version**: C# / .NET 9  
**Primary Dependencies**: Marten (event store), MediatR (CQRS), FluentValidation, Blazor WebAssembly  
**Storage**: PostgreSQL via Marten event store  
**Testing**: xUnit with custom verification helpers  
**Target Platform**: Web (Blazor WebAssembly + ASP.NET Core server)  
**Project Type**: Web application (modular monolith)  
**Performance Goals**: Sub-second API key validation, 99.9% uptime under 1000 concurrent requests  
**Constraints**: HTTPS required for all API calls, cryptographically secure key generation, keys not stored in plain text  
**Scale/Scope**: Support up to 1000 concurrent API key validations, per-user key management

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The following checks MUST be validated and documented in the plan before research proceeds:

- **Tests**: Unit tests for API key domain logic (creation, revocation, validation) and integration tests for authentication flow. At least one integration test covering the full API key lifecycle.
- **Observability**: Structured logging for API key usage attempts, authentication success/failure metrics, and audit trail for key creation/revocation events.
- **Security & Privacy**: API keys generated cryptographically secure, hashed in storage, HTTPS enforced for all API calls, keys never displayed after creation, audit logging for security events.
- **Code Quality**: Conformance with repository conventions (one public type per file, StyleCop rules, no XML documentation comments, zero compiler warnings, Blazor components split into `.razor` + `.razor.cs`).
  - Commands/Queries placement: API key commands/queries in `Spamma.Modules.UserManagement.Client`, handlers in `Spamma.Modules.UserManagement`.
  - Project additions: No new projects required - feature fits within existing UserManagement module.
- **CI Compatibility**: Feature builds and tests in CI environment, frontend assets compile successfully.

**Gate Evaluation**: All gates pass. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/001-api-key-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── api-keys.yaml    # OpenAPI specification for API key endpoints
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── modules/
│   ├── Spamma.Modules.UserManagement/
│   │   ├── Spamma.Modules.UserManagement.Client/     # Commands, Queries, DTOs
│   │   │   ├── Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── ApiKeys/                       # NEW: CreateApiKeyCommand, RevokeApiKeyCommand
│   │   │   │   └── Queries/
│   │   │   │       └── ApiKeys/                       # NEW: GetApiKeysQuery
│   │   │   └── Domain/                                # NEW: ApiKey entity, events
│   │   └── Spamma.Modules.UserManagement/            # Server implementation
│   │       ├── Application/
│   │       │   ├── CommandHandlers/
│   │       │   │   └── ApiKeys/                       # NEW: CreateApiKeyCommandHandler, RevokeApiKeyCommandHandler
│   │       │   ├── QueryProcessors/
│   │       │   │   └── ApiKeys/                       # NEW: GetApiKeysQueryProcessor
│   │       │   ├── Validators/                        # NEW: API key validation rules
│   │       │   └── Authorizers/                       # NEW: API key authorization
│   │       ├── Domain/
│   │       │   └── ApiKeys/                           # NEW: ApiKey aggregate, events
│   │       ├── Infrastructure/
│   │       │   ├── Projections/                       # NEW: API key projections
│   │       │   ├── Repositories/                      # NEW: IApiKeyRepository
│   │       │   ├── Services/                          # NEW: ApiKeyAuthenticationHandler, ApiKeyValidationService
│   │       │   └── Migrations/                        # NEW: Database schema for API keys
│   │       └── Tests/                                 # NEW: Unit and integration tests
│   └── Spamma.App/
│       ├── Spamma.App/                                # Server-side authentication
│       │   ├── Controllers/                           # Auto-generated by BluQube from commands/queries
│       │   ├── Infrastructure/
│       │   │   ├── Middleware/                        # NEW: API key authentication middleware
│       │   │   └── Services/                          # NEW: ApiKeyAuthenticationService
│       │   └── Pages/                                 # Server-side static pages (if needed)
│       └── Spamma.App.Client/                         # Client-side UI
│           └── Components/
│               └── ApiKeys/                           # NEW: ApiKeyManager.razor, ApiKeyList.razor, API key management UI pages
```

**Structure Decision**: Feature fits within existing UserManagement module following the dual-project pattern. API key functionality is closely related to user authentication and management, so it belongs in the UserManagement module rather than creating a new module.
