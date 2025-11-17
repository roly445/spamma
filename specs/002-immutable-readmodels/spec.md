# Feature Specification: Immutable ReadModels

**Feature Branch**: `002-immutable-readmodels`  
**Created**: November 17, 2025  
**Status**: Draft  
**Input**: User description: "Make all readmodels immutable with private setters"

## Overview

This feature standardizes all readmodel classes (projection targets) to use immutable property initialization patterns with private setters. Readmodels are used by Marten as read-optimized data structures built from domain events through projections. Making them immutable improves code safety, thread-safety guarantees, and enforces the event-sourcing pattern where state changes should only occur through projection logic (event handlers).

## Clarifications

### Session 2025-11-17

- Q: Should implementation include explicit validation that Marten's `Patch()` API works with private setters before proceeding, or validate only during integration testing? → A: Add explicit unit test early to validate Marten Patch works with private setters before modifying readmodels at scale

## User Scenarios & Testing

### User Story 1 - Enforce Immutability at Compile Time (Priority: P1)

As a developer, I want readmodel properties to have private setters so that the compiler prevents accidental direct property mutation outside of projection logic, ensuring state changes only flow through event projections.

**Why this priority**: This is the core requirement. Without immutable properties, the entire goal of event-sourced readmodels is undermined. Developers could bypass projections and mutate state directly, breaking the audit trail and event-sourcing guarantees.

**Independent Test**: A build should fail with a compile error if any code attempts to set a public property on a readmodel after it's created. This can be tested independently by attempting to mutate a readmodel property and verifying the compilation fails.

**Acceptance Scenarios**:

1. **Given** a readmodel is defined with private setters, **When** code attempts to set a public property directly (outside the class), **Then** compilation fails with a clear error
2. **Given** a readmodel is being instantiated in a projection's `Create` method, **When** properties are set during object initialization, **Then** the initialization succeeds
3. **Given** a readmodel exists in the codebase, **When** the codebase is analyzed, **Then** no public property has a public setter

---

### User Story 2 - Enable Marten Patching with Private Setters (Priority: P1)

As a Marten ORM user, I want projections using `IDocumentOperations.Patch()` to continue working with readmodels that have private setters, so that event-driven updates to readmodel state work seamlessly without requiring public setters.

**Why this priority**: Marten's `Patch()` API requires the ability to set properties, even if they're private. This is critical infrastructure that must work correctly. If patching breaks, projections cannot update readmodel state when events occur.

**Independent Test**: Update a projection that uses `Patch()` operations on a readmodel property with a private setter, deploy the code, trigger the event, and verify the readmodel state is updated correctly via a query.

**Acceptance Scenarios**:

1. **Given** a projection with `Set(x => x.PropertyName, value)` operations on a private-setter property, **When** the event is sourced, **Then** the readmodel is updated correctly without errors
2. **Given** a readmodel with nested list properties (e.g., `List<T>` with private setter), **When** the projection patches that property, **Then** the collection is replaced correctly
3. **Given** a readmodel with nullable properties having private setters, **When** the projection sets those properties to null and non-null values, **Then** the values persist correctly

---

### User Story 3 - Standardize Readmodel Construction (Priority: P2)

As a codebase maintainer, I want all readmodels to follow a consistent immutable pattern so that all developers understand the readmodel initialization pattern and can easily add new readmodels.

**Why this priority**: Once the core immutability is enforced, standardizing construction patterns reduces cognitive load and prevents inconsistent implementations. This improves code review efficiency and onboarding.

**Independent Test**: A code review of a new readmodel implementation shows it follows the immutable pattern without requiring corrections. Can be tested by adding a new readmodel and verifying it passes code review without property-setter related comments.

**Acceptance Scenarios**:

1. **Given** a new readmodel is added to the codebase, **When** reviewed for compliance, **Then** all properties follow the immutable pattern with private setters
2. **Given** a readmodel property is initialized with a default collection (e.g., `= new()`), **When** the readmodel is used, **Then** the collection is properly initialized and accessible
3. **Given** a developer implements a new readmodel from scratch, **When** they follow the template/pattern, **Then** no modifications are needed during code review

---

### User Story 4 - Maintain Backward Compatibility (Priority: P2)

As a system operator, I want the immutability changes to maintain compatibility with existing Marten event stores and querying logic so that deployments proceed without data migration.

**Why this priority**: While important, this is secondary to the core immutability implementation. However, it's essential to prevent breaking existing systems and requiring database migrations.

**Independent Test**: Deploy code with immutable readmodels to a system with existing Marten-stored events, then query readmodels and verify they hydrate correctly with the same data as before.

**Acceptance Scenarios**:

1. **Given** a Marten event store with existing readmodel documents, **When** the code is deployed with immutable readmodels, **Then** existing documents load and deserialize correctly
2. **Given** a projection targeting an immutable readmodel, **When** the projection replays events, **Then** all existing documents are updated correctly without errors
3. **Given** a Marten query on a readmodel with private setters, **When** the query executes, **Then** the results are complete and accurate

---

### Edge Cases

- What happens when a readmodel has a reference to a mutable collection that should be part of the projection (e.g., `List<Guid> ModeratedDomains`)? The collection itself can be mutable, but the property setter should be private to ensure projections control when the collection is replaced.
- How are readmodels with multiple initialization patterns handled (some that need parameterized constructors vs. object initializers)? Favor the simpler object initializer pattern with private setters for consistency.
- What about readmodels that are used both for projections and as DTOs in API responses? Readmodels in `Infrastructure/ReadModels/` are internal projection targets; API response types should be separate DTOs in the `.Client` project.

---

## Requirements

### Functional Requirements

- **FR-001**: All readmodel classes in `Infrastructure/ReadModels/` directories MUST have only private setters on public properties
- **FR-002**: Readmodel properties MUST be initializable via object initializers in projection `Create` methods (e.g., `new ReadModel { PropA = value, PropB = value }`)
- **FR-003**: Marten's `IDocumentOperations.Patch()` operations MUST successfully set private-setter properties without runtime errors
- **FR-004**: Readmodels with collection properties (e.g., `List<T>`, `Dictionary<K,V>`) MUST initialize collections to empty instances by default (e.g., `= new()`)
- **FR-005**: Readmodel properties that expose nested complex types (e.g., domain objects) MUST have private setters to enforce immutability boundaries
- **FR-006**: All projection methods that hydrate readmodels MUST use object initializer syntax or inline property assignment during creation; no direct assignment after instantiation
- **FR-007**: Existing Marten event store documents and projections MUST continue to deserialize and function correctly after readmodel immutability changes
- **FR-008**: No new public constructors with parameters MUST be added to readmodels (use object initializers exclusively for consistency)
- **FR-009**: A unit test MUST validate that Marten's `IDocumentOperations.Patch()` API successfully sets properties with private setters before proceeding with bulk readmodel modifications (proof of concept validation)

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code MUST compile with zero warnings. Any new warnings must be addressed before merging.
- **CQ-002**: Public types MUST use clear intent naming. XML documentation comments (`///`) are NOT required and MUST NOT be used for public API intent — code clarity and naming SHOULD provide intent.
- **CQ-003**: All readmodels MUST have private property setters; no public setters allowed (enforced by StyleCop SA1206 and code review)
- **CQ-004**: No property should have a public getter with a public setter (SA1206 violations)
- **CQ-005**: Readmodels in modules are NOT public APIs and should not be referenced outside their module except through projections and queries

### Key Entities

- **ReadModel**: A class used as a Marten document that represents a read-optimized view of domain state. Examples: `UserLookup`, `DomainLookup`, `SubdomainLookup`, `EmailLookup`, `PasskeyProjection`, `ApiKeyProjection`, `ChaosAddressLookup`, `CampaignSummary`
- **Projection**: A Marten `EventProjection` that handles domain events and updates readmodels via the `Create` method (initial state) or `Project` methods (updates via `Patch`)
- **Event**: A domain event that flows through the projection to update readmodel state. Events are immutable and represent something that happened in the past.

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: 100% of readmodel classes in `Infrastructure/ReadModels/` directories have only private property setters (zero public setters)
- **SC-002**: All projection `Create` methods successfully initialize readmodels using object initializer syntax without compilation errors
- **SC-003**: All `Patch()` operations in projection methods update private-setter properties correctly (verified by integration tests)
- **SC-004**: Existing Marten-stored readmodel documents deserialize without errors after code deployment (verified in integration test environment)
- **SC-005**: No compilation warnings related to property access in readmodels or projections after changes
- **SC-006**: Code review checklist includes verification of readmodel immutability patterns, with all PRs passing this check before merge

---

## Implementation Scope

### In Scope

- **Validation spike**: Create unit test to prove Marten's `Patch()` API works with private setters
- Modifying all readmodel class property declarations to use private setters
- Ensuring projections work with private-setter readmodels using Marten's `Patch()` API
- Updating existing readmodel initializations to use object initializer syntax
- Verifying Marten serialization/deserialization compatibility
- Unit and integration tests validating the immutability changes

### Out of Scope

- Creating new readmodels (follow the pattern once established)
- Refactoring projection logic (only changes needed to support immutable readmodels)
- Modifying event classes or domain aggregates
- Creating separate DTO types (existing API responses can continue to use readmodels; future API response types should be in `.Client` projects)
- Renaming or restructuring readmodel classes or their properties

---

## Dependencies & Assumptions

### Dependencies

- **Marten ORM**: Must continue to support property setting on private setters during patching and deserialization
- **Existing Projections**: All existing projection logic must be compatible with immutable readmodels
- **Serialization**: JSON serialization of readmodels for API responses must work with private setters (handled by .NET serializers by default)

### Assumptions

- Marten's JSON deserialization (`IDocumentOperations.Patch()` and direct document hydration) works correctly with private setters (standard .NET serialization behavior)
- Readmodels are primarily used for read operations and internal projections; they are not the primary contract for external APIs
- Existing test suites for projections will validate immutability changes without requiring new test infrastructure
- All readmodels currently use auto-properties (backing fields are generated); no manual backing field implementations need special handling
- Default collection initialization (e.g., `= new()`) is preferred over null for list properties to simplify projection logic

---

## Acceptance Criteria Checklist

- [ ] All readmodel classes have private property setters
- [ ] No readmodel properties have public setters
- [ ] All projection `Create` methods use object initializer syntax
- [ ] All `Patch()` operations work correctly with private-setter properties
- [ ] Marten serialization/deserialization tests pass
- [ ] Existing readmodel queries return correct data without changes to query logic
- [ ] No new compilation warnings or errors
- [ ] Code review checklist updated to include immutability validation
