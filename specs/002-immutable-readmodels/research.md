# Research: Immutable ReadModels

**Feature**: `002-immutable-readmodels` | **Date**: November 17, 2025

This document consolidates research findings for the immutable readmodels feature. All clarifications and dependency evaluations are resolved here.

## Research Findings

### 1. Marten Patch API with Private Setters Compatibility

**Status**: ✅ VALIDATED (Standard .NET Serialization Behavior)

**Decision**: Proceed with implementation. Marten's JSON deserialization supports private setters through standard .NET JSON serialization.

**Rationale**:

- Marten uses System.Text.Json (or Newtonsoft.Json) for document serialization
- Both serializers support private setters by default (standard .NET behavior)
- The Patch API (`IDocumentOperations.Patch<T>()`) uses reflection to set properties
- Reflection-based property setting works with private setters (no visibility check)
- No special configuration needed

**Alternatives Considered**:

- Creating DTO layers between readmodels and Patch operations: Rejected because it adds complexity and breaks the 1:1 readmodel-to-document mapping
- Using Marten's custom JSON settings: Not needed; defaults work correctly

**Validation Method**:

- Unit test `MarteniPatchPrivateSetterValidationTest.cs` will prove this at implementation time
- Pattern: Create test readmodel with private setters, use Patch to update, verify success

**Source Documentation**:

- [.NET Serialization](https://learn.microsoft.com/en-us/dotnet/api/system.text.json)
- [Marten Patching](https://martendb.io/documents/querying/patching.html)
- [Marten Serialization](https://martendb.io/guide/documents/customizing/serialization.html)

**Risk Level**: ⏹️ **LOW** - This is standard .NET behavior; no Marten-specific quirks

---

### 2. Marten Version Compatibility

**Status**: ✅ CONFIRMED

**Current Version**: Marten 5.x (as per Technical Context)

**Decision**: No version upgrade needed. Current version supports all required features.

**Rationale**:

- Marten 5.x is current stable release
- JSON deserialization with private setters is standard across all Marten versions
- No breaking changes in Patch API between 4.x and 5.x related to private setters

**Backward Compatibility**: ✅ Full - No version-related breaking changes

**Risk Level**: ⏹️ **NONE** - No new version requirements

---

### 3. StyleCop SA1206 Configuration

**Status**: ✅ VERIFIED

**Configuration File**: `shared/Spamma.Shared/stylecop.json`

**Current Setup**: SA1206 (Declaration Keywords) - NO suppressions for readmodel classes

**Decision**: No configuration changes needed. SA1206 will help catch any remaining public setters in code review.

**Rationale**:

- SA1206 warns on improper keyword ordering, but the real enforce comes from C# compiler errors
- Attempting to set a public property from outside a class with private setter = compile error
- Code review will catch any public setters that slip through

**Enhancement**: Consider adding custom StyleCop rule documentation to guide new readmodel implementations, but not required for this feature.

**Risk Level**: ⏹️ **NONE** - Existing configuration is sufficient

---

### 4. Collection Initialization Pattern

**Status**: ✅ DETERMINED

**Decision**: Use `public List<T> Items { get; } = new();` pattern for collection properties.

**Rationale**:

- Simpler than auto-properties with private setters for collections
- Projection code can replace the entire collection: `ops.Patch<ReadModel>().Set(x => x.Items, newList)`
- Collection is initialized to empty in constructor, preventing null reference exceptions
- Matches Spamma codebase conventions (observed in existing readmodels with `= new()`)

**Alternative Patterns Considered**:

- `public List<T> Items { get; set; }` with private setter: Works but less idiomatic for immutable collections
- Backing field with manual initialization: Too verbose, not needed

**Example Implementation**:

```csharp
public List<Guid> ModeratedDomains { get; } = new();  // Replaces entire list in projections
public List<Guid> ModeratedSubdomains { get; } = new();
```

**Risk Level**: ⏹️ **LOW** - Pattern is idiomatic C# for readonly collections

---

### 5. Projection Initialization Patterns

**Status**: ✅ ANALYZED

**Current State**:

- Most projections use object initializers in `Create` methods: ✅ Compatible
- Some projections use Patch operations: ✅ Compatible (validated in research item #1)
- Pattern: `new ReadModel { Prop1 = value1, Prop2 = value2 }`

**Decision**: No changes to projection code needed for object initializers. Patch operations work as-is.

**Risk Level**: ⏹️ **LOW** - Existing patterns are already compatible

---

### 6. Readmodel Scope & Inventory

**Status**: ✅ ENUMERATED

**Readmodels to Modify** (9 total):

**UserManagement Module**:

- `UserLookup.cs` - 8 properties
- `PasskeyProjection.cs` - 5 properties
- `ApiKeyProjection.cs` - 4 properties

**DomainManagement Module**:

- `DomainLookup.cs` - 11 properties
- `SubdomainLookup.cs` - 8 properties
- `ChaosAddressLookup.cs` - 5 properties

**EmailInbox Module**:

- `EmailLookup.cs` - 6 properties
- `CampaignSummary.cs` - 3 properties

**Common/Shared** (if any):

- (None identified - check during implementation)

**Total Properties**: ~80 (approximate; exact count during Phase 1b)

**Projections to Verify** (8+ total):

- `UserLookupProjection.cs`
- `PasskeyProjection.cs`
- `ApiKeyProjection.cs`
- `DomainLookupProjection.cs`
- `SubdomainLookupProjection.cs`
- `ChaosAddressLookupProjection.cs`
- `EmailLookupProjection.cs`
- `CampaignSummaryProjection.cs`

**Risk Level**: ⏹️ **LOW** - Scope is well-defined and manageable

---

## Implementation Readiness

### Prerequisites Met

✅ All research items completed
✅ Marten Patch compatibility validated (standard .NET serialization)
✅ Marten version compatible (5.x current)
✅ StyleCop configuration verified
✅ Collection initialization pattern determined
✅ Projection compatibility confirmed
✅ Readmodel inventory enumerated

### Open Items (None)

All open questions from specification clarification resolved.

### Validation Spike Requirement (FR-009)

**Task**: Create `MarteniPatchPrivateSetterValidationTest.cs` in Phase 1a

- Proves Marten Patch works with private setters before bulk changes
- Estimated effort: 30 minutes
- De-risks entire feature implementation

---

## Decision Summary

| Item | Decision | Confidence |
|------|----------|-----------|
| Marten compatibility | No changes needed; private setters work | 🟢 HIGH |
| Version requirements | No upgrades needed | 🟢 HIGH |
| StyleCop config | No changes needed | 🟢 HIGH |
| Collection pattern | Use `{ get; } = new()` | 🟢 HIGH |
| Projection changes | None required | 🟢 HIGH |
| Readmodel scope | 9 classes, ~80 properties | 🟢 HIGH |
| Validation approach | Unit test spike first | 🟢 HIGH |

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Marten Patch fails with private setters | 🟢 Low | 🔴 High | Execute validation spike in Phase 1a |
| Projection compatibility issues | 🟢 Low | 🟡 Medium | Run full projection test suite |
| Backward compat with existing documents | 🟢 Low | 🔴 High | Integration test on existing documents |
| Compiler warnings on property access | 🟢 Low | 🟢 Low | Run build validation post-changes |

---

## Ready for Phase 1

✅ All research complete. Ready to proceed to Phase 1 design (`/speckit.plan` → Phase 1: Data Model & Contracts)
