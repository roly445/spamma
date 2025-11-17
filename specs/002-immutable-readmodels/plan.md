# Implementation Plan: Immutable ReadModels

**Branch**: `002-immutable-readmodels` | **Date**: November 17, 2025 | **Spec**: [Immutable ReadModels Spec](spec.md)
**Input**: Feature specification from `/specs/002-immutable-readmodels/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Standardize all readmodel classes in the Spamma modular monolith to use immutable property initialization with private setters. This enforces the event-sourcing pattern by preventing direct property mutation outside of projection logic. The implementation involves modifying 9 readmodel classes across 4 modules, validating Marten's Patch API compatibility with private setters, updating projection initialization patterns, and adding comprehensive tests. The change is backward-compatible with existing Marten event stores and requires no database migrations.

## Technical Context

**Language/Version**: C# 12.0 (.NET 9)

**Primary Dependencies**:

- Marten 5.x (PostgreSQL event sourcing and document store)
- MediatR (CQRS pattern)
- FluentValidation (command/query validation)

**Storage**: PostgreSQL (via Marten event store)

**Testing**: xUnit + Moq (existing test framework)

**Target Platform**: .NET modular monolith (backend only; no frontend changes)

**Project Type**: Server-side modular monolith with CQRS and event sourcing

**Performance Goals**: No performance impact; readonly properties have same performance as public setters

**Constraints**:

- Must maintain 100% backward compatibility with existing Marten documents
- Must not break any existing projections or queries
- Zero compiler warnings (StyleCop SA1206 already configured)

**Scale/Scope**:

- 9 readmodel classes across 4 modules (UserManagement, DomainManagement, EmailInbox, Common)
- ~80 properties to convert to private setters
- 8+ existing projections to verify compatibility

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The following checks MUST be validated and documented in the plan before research proceeds:

✅ **Tests**: Unit tests will validate compile-time immutability (property mutation attempted on readonly instance). Integration tests will verify Patch operations work correctly on all readmodels. Existing projection tests will be run to confirm backward compatibility.

✅ **Observability**: No observability impact. Readmodels are read-only data structures; no new logging/tracing needed beyond existing projection instrumentation.

✅ **Security & Privacy**: No security implications. This change strengthens the security posture by preventing accidental bypass of projection-mediated state updates. No data retention or access control changes.

✅ **Code Quality**:

- All changes will follow StyleCop rules (SA1206 already configured to warn on public setters)
- No XML documentation comments will be added (class names and intent are clear)
- Zero new compiler warnings will be introduced
- No new projects required (changes to existing Infrastructure/ReadModels/ classes only)
- All readmodels are internal to their modules; no public API contract changes

✅ **CI Compatibility**:

- Build-only changes; no frontend asset compilation needed
- Standard .NET build + test commands apply
- xUnit tests run in CI as configured
- No dependencies on external services beyond existing PostgreSQL container

## Project Structure

### Documentation (this feature)

```text
specs/002-immutable-readmodels/
├── plan.md                      # This file
├── research.md                  # Phase 0: Research findings (PENDING)
├── data-model.md                # Phase 1: Data model analysis (PENDING)
├── quickstart.md                # Phase 1: Implementation quickstart (PENDING)
├── contracts/                   # Phase 1: (No contracts - infrastructure change)
└── checklists/
    └── requirements.md          # Quality validation checklist
```

### Source Code (repository root - No new projects)

```text
# Existing modular monolith structure - ONLY Infrastructure/ReadModels/ folders modified

src/modules/
├── Spamma.Modules.UserManagement/
│   └── Infrastructure/
│       └── ReadModels/          # MODIFY: UserLookup.cs, PasskeyProjection.cs, ApiKeyProjection.cs
├── Spamma.Modules.DomainManagement/
│   └── Infrastructure/
│       └── ReadModels/          # MODIFY: DomainLookup.cs, SubdomainLookup.cs, ChaosAddressLookup.cs
└── Spamma.Modules.EmailInbox/
    └── Infrastructure/
        └── ReadModels/          # MODIFY: EmailLookup.cs, CampaignSummary.cs

tests/
├── Spamma.Modules.UserManagement.Tests/
│   └── Infrastructure/
│       └── Projections/         # ADD: MarteniPatchPrivateSetterValidationTest.cs
├── Spamma.Modules.DomainManagement.Tests/
│   └── Infrastructure/
│       └── Projections/         # VERIFY: Existing projection tests pass
└── Spamma.Modules.EmailInbox.Tests/
    └── Infrastructure/
        └── Projections/         # VERIFY: Existing projection tests pass
```

**Structure Decision**: This is a refactoring of existing readmodel infrastructure within the modular monolith. No new projects are created. Changes are localized to:

- 9 readmodel class files (modify property setters)
- 8+ projection test files (verify compatibility)
- 1 new unit test file (Marten Patch validation spike)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected. All Constitution Check items passed (✅). The change is a straightforward refactoring with no architectural complexity or constraint violations.

---

## Phase 0: Research & Validation

### Research Tasks

1. **Task: Validate Marten Patch API with Private Setters (FR-009)**
   - Create unit test using a simple readmodel (e.g., UserLookup) with private setters
   - Use Marten's `IDocumentOperations.Patch<T>().Set(x => x.Property, value)` pattern
   - Verify JSON serialization (Marten's serializer) handles private setters on hydration
   - Document findings in `research.md`
   - **Acceptance**: Test passes; Patch operations work without modification

2. **Task: Marten Version Compatibility (Dependencies)**
   - Confirm current Spamma Marten version (5.x)
   - Check Marten documentation for private setter support in JSON deserialization
   - Verify no version upgrade needed
   - **Acceptance**: Documented in research.md with version info

3. **Task: StyleCop SA1206 Configuration Review**
   - Verify SA1206 rule is correctly configured to catch public setters
   - Confirm no overrides or suppressions exist for readmodel classes
   - **Acceptance**: Configuration file reviewed; no changes needed

**Research Output**: `research.md` with all findings and validation results

---

## Phase 1: Design & Implementation Strategy

### Design Tasks

1. **Task: Data Model Analysis (data-model.md)**
   - Enumerate all 9 readmodel classes and their properties
   - Identify property types (auto-properties, backing fields, collections, nested types)
   - Document initialization patterns (object initializers, constructors, default values)
   - Identify any special cases (collections with custom initialization, complex types)
   - **Acceptance**: Complete list with no readmodels missed

2. **Task: Conversion Pattern Documentation (quickstart.md)**
   - Document the pattern: `public string Name { get; set; }` → `public string Name { get; private set; }`
   - Show collection initialization pattern: `public List<T> Items { get; } = new();`
   - Provide code examples for each pattern variation
   - Document projection Create/Patch method compatibility
   - **Acceptance**: Clear guidance for developers to apply consistently

3. **Task: Projection Compatibility Checklist (quickstart.md)**
   - List all 8+ projections that target readmodels
   - Verify each projection's Create method uses object initializer syntax
   - Verify each projection's Patch operations work with private setters
   - Mark any that need adjustment
   - **Acceptance**: All projections verified compatible or updated

### Implementation Strategy

1. **Phase 1a: Validation Spike** (FR-009)
   - Create `MarteniPatchPrivateSetterValidationTest.cs` in UserManagement.Tests
   - Test Patch operations on a simple readmodel with private setters
   - Run test to confirm compatibility
   - If passes: proceed to Phase 1b
   - If fails: investigate Marten configuration or version issues

2. **Phase 1b: Apply Changes to Readmodels**
   - Convert all properties in 9 readmodel classes to private setters
   - Follow pattern: `{ get; private set; }` for all public properties
   - Initialize collection properties: `public List<T> Items { get; } = new();`
   - Apply changes in this order:
     1. UserManagement: UserLookup, PasskeyProjection, ApiKeyProjection
     2. DomainManagement: DomainLookup, SubdomainLookup, ChaosAddressLookup
     3. EmailInbox: EmailLookup, CampaignSummary

3. **Phase 1c: Verify Projections**
   - Run all existing projection tests
   - Verify no compilation errors related to property access
   - Confirm all Patch operations update correctly
   - Confirm all object initializers work
   - No test changes needed if all pass

4. **Phase 1d: Verify Serialization**
   - Build solution; ensure zero compiler warnings
   - Run integration tests for backward compatibility (SC-004)
   - Confirm existing Marten-stored documents hydrate correctly
   - Confirm queries return correct data

**Design Output**: `data-model.md`, `quickstart.md`, verification checklist

---

## Next Steps (Phase 2)

After Phase 1 design completion:

- Run `/speckit.tasks` to generate detailed task breakdown (`tasks.md`)
- Begin implementation following the quickstart guide
- Execute tasks in the prescribed order (validation spike first, then readmodel conversion, then verification)
