# Quickstart Guide: Immutable ReadModels Pattern

**Feature**: `002-immutable-readmodels` | **Date**: November 17, 2025

---

## Overview

This guide walks you through converting readmodels to use immutable properties with private setters, ensuring compile-time safety while maintaining full compatibility with Marten projections.

**Key Pattern**: All readmodel properties become immutable from outside the projection, enforcing that state changes only occur through projection logic.

---

## Pattern Examples

### Pattern 1: Scalar Properties with Private Setters

Use for: Strings, Guids, DateTimes, bools, enums, ints, etc.

**Before (Mutable)**:
```csharp
public class UserLookup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsSuspended { get; set; }
    public SystemRole SystemRole { get; set; }
}
```

**After (Immutable)**:
```csharp
public class UserLookup
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string EmailAddress { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsSuspended { get; private set; }
    public SystemRole SystemRole { get; private set; }
}
```

**Why**:
- Prevents accidental mutations outside projection
- Preserves all initialization defaults
- Works seamlessly with Marten's JSON deserialization (uses reflection, bypasses visibility)
- Projection code can still set via object initializers or Patch operations

---

### Pattern 2: Collection Properties (Read-Only Collections)

Use for: List<T>, IList<T>, etc.

**Before (Mutable)**:
```csharp
public class UserLookup
{
    public List<Guid> ModeratedDomains { get; set; } = new();
    public List<Guid> ModeratedSubdomains { get; set; } = new();
    public List<Guid> ViewableSubdomains { get; set; } = new();
}
```

**After (Immutable)**:
```csharp
public class UserLookup
{
    public List<Guid> ModeratedDomains { get; } = new();
    public List<Guid> ModeratedSubdomains { get; } = new();
    public List<Guid> ViewableSubdomains { get; } = new();
}
```

**Why**:
- Getter-only (`{ get; }`) prevents replacing the entire collection reference
- The list itself remains mutable (code can call `.Add()`, `.Remove()`)
- Projections replace entire list via Patch: `ops.Patch<UserLookup>().Set(x => x.ModeratedDomains, newList)`
- Prevents accidental list replacement outside projection

**Note**: If you need a truly immutable collection (cannot add/remove), use `IReadOnlyList<T>` (requires more changes to projections).

---

### Pattern 3: Nullable Properties

Use for: DateTime?, int?, etc.

**Before**:
```csharp
public class UserLookup
{
    public DateTime? LastLoginAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
}
```

**After**:
```csharp
public class UserLookup
{
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
}
```

**Why**: Same as scalar properties - prevents mutation, allows Marten compatibility.

---

## Conversion Checklist

For each readmodel, follow these steps:

### Step 1: Identify Properties
- [ ] List all public auto-properties (e.g., `public string Name { get; set; }`)
- [ ] Identify collections (e.g., `public List<T> Items { get; set; }`)
- [ ] Note any properties with default initialization (e.g., `= string.Empty`, `= new()`)

### Step 2: Apply Pattern
- [ ] Scalar/nullable properties: Add `private set;` before existing `set;`
  - **Before**: `public string Name { get; set; }`
  - **After**: `public string Name { get; private set; }`
- [ ] Collections: Replace `set;` with nothing (get-only)
  - **Before**: `public List<Guid> Items { get; set; } = new();`
  - **After**: `public List<Guid> Items { get; } = new();`
- [ ] Preserve all default initializations (e.g., `= string.Empty`, `= new()`)

### Step 3: Verify No Breaking Changes
- [ ] Object initializers in projections still work (they do - `{ get; private set; }` allows initialization)
- [ ] Patch operations in projections still work (they do - reflection bypasses visibility)
- [ ] No Marten configuration changes needed
- [ ] No database migrations needed (JSON compatibility unchanged)

### Step 4: Test
- [ ] Build solution: `dotnet build Spamma.sln`
- [ ] Verify zero compiler warnings
- [ ] Run projection tests: `dotnet test tests/Spamma.Modules.X.Tests/Infrastructure/Projections/`
- [ ] Verify all tests pass without modification

---

## Projection Compatibility

### Object Initializers (Used in Projection `Create` Methods)

**Already compatible - no changes needed**:

```csharp
// Projection Create method - works as-is
public ReadModel Create(DomainCreatedEvent @event)
{
    return new DomainLookup
    {
        Id = @event.Id,                           // Private setter - still works!
        DomainName = @event.DomainName,          // Private setter - still works!
        Status = DomainStatus.Pending,           // Private setter - still works!
        Subdomains = new(),                      // Getter-only collection - works!
    };
}
```

**Why it works**: Object initializers in C# bypass visibility restrictions during construction. This is by design and is a standard C# feature.

---

### Patch Operations (Used in Projection Event Handlers)

**Already compatible - no changes needed**:

```csharp
// Projection Patch method - works as-is
public void Apply(DomainVerifiedEvent @event, IDocumentOperations ops)
{
    ops.Patch<DomainLookup>(@event.DomainId)
        .Set(x => x.Status, DomainStatus.Active)        // Private setter - Patch uses reflection!
        .Set(x => x.VerifiedAt, @event.VerifiedAt)      // Private setter - still works!
        .Set(x => x.MxRecordVerified, true);             // Private setter - still works!
}

// Multiple patches work too
public void Apply(DomainSubdomainsUpdatedEvent @event, IDocumentOperations ops)
{
    ops.Patch<DomainLookup>(@event.DomainId)
        .Set(x => x.Subdomains, @event.NewSubdomains);  // Getter-only collection - Patch works!
}
```

**Why it works**: Marten's Patch API uses .NET reflection to set properties, which bypasses visibility restrictions. This is standard .NET behavior validated in the Phase 1 spike test.

---

## Common Patterns in Spamma Codebase

### UserManagement Module

**UserLookup**:
- 3 collection properties → Use getter-only `{ get; } = new()`
- 9 scalar/nullable properties → Use private setters `{ get; private set; }`
- Projections use object initializers in Create and Patch in event handlers

**PasskeyProjection & ApiKeyProjection**:
- All scalar properties → Use private setters
- No collections → Straightforward conversion

### DomainManagement Module

**DomainLookup**:
- 2 collection properties (nested objects) → Use getter-only `{ get; } = new()`
- 9 scalar/nullable properties → Use private setters
- Projections replace entire collections via Patch

**SubdomainLookup & ChaosAddressLookup**:
- All scalar properties → Use private setters
- No collections → Straightforward conversion

### EmailInbox Module

**EmailLookup & CampaignSummary**:
- All scalar properties → Use private setters
- No collections → Straightforward conversion

---

## Edge Cases & Decisions

### Edge Case 1: What if projection code tries to mutate the list?

**Code that breaks**:
```csharp
public void Apply(DomainSubdomainAddedEvent @event, IDocumentOperations ops)
{
    ops.Patch<DomainLookup>(@event.DomainId)
        .Set(x => x.Subdomains.Add(newSubdomain));  // ❌ BREAKS - can't call method on Set() value
}
```

**Correct pattern** (replace entire list):
```csharp
public void Apply(DomainSubdomainAddedEvent @event, IDocumentOperations ops)
{
    // Load current subdomains, add new one, replace entire list
    var currentSubdomains = /* fetch current */;
    currentSubdomains.Add(newSubdomain);
    
    ops.Patch<DomainLookup>(@event.DomainId)
        .Set(x => x.Subdomains, currentSubdomains);  // ✅ Correct - replace entire list
}
```

**Decision for Spamma**: All projection code already follows the "replace entire collection" pattern. No code changes needed.

---

### Edge Case 2: Backward Compatibility with Existing Marten Documents

**Question**: Can Marten deserialize old documents (with public setters) into new readmodels (with private setters)?

**Answer**: ✅ **Yes**. Marten uses reflection to deserialize, which bypasses visibility restrictions.

**Validation**: Phase 1 spike test confirms JSON → object with private setters works perfectly.

**Migration Needed**: None. Existing Marten documents continue to work without changes.

---

### Edge Case 3: What if a readmodel needs initialization logic?

**Scenario**: A readmodel needs custom constructor logic.

**Current State**: No readmodels in Spamma have constructors (all use property initialization).

**Decision**: If needed in future, use:
```csharp
public class ReadModel
{
    // Property initializer (works with private setters)
    public List<Guid> Items { get; } = new();
    
    // Custom constructor (if needed in future)
    public ReadModel()
    {
        // Initialization logic here
    }
}
```

**Status for this feature**: No constructors needed.

---

## Verification Steps

### Before Starting
- [ ] Phase 1 validation spike passed (confirms Marten compatibility)
- [ ] All 8 readmodels identified in data-model.md
- [ ] Existing projection tests reviewed

### During Conversion
- [ ] Modify one readmodel at a time
- [ ] Run `dotnet build Spamma.sln` after each change
- [ ] Fix any compile errors immediately (should be none)
- [ ] Check for StyleCop warnings (should be none)

### After All Conversions
- [ ] Run full build: `dotnet build Spamma.sln`
- [ ] Run all projection tests: `dotnet test tests/`
- [ ] Verify zero test regressions (all tests pass as-is)
- [ ] Create backward compatibility test (verify existing documents still load)

---

## Helpful Tips

### Tip 1: Use Find & Replace for Quick Conversion

**Search for**:
```
public (\w+) (\w+) \{ get; set; \}
```

**Replace with**:
```
public $1 $2 { get; private set; }
```

**Caution**: Use this carefully - verify each match before replacing.

### Tip 2: Handle Collections Separately

Collections need different treatment:

**Search for**:
```
public List<(\w+)> (\w+) \{ get; set; \} = new\(\);
```

**Replace with**:
```
public List<$1> $2 { get; } = new();
```

### Tip 3: Build After Each File

After converting each readmodel file:
```powershell
dotnet build Spamma.sln --no-restore
```

This catches any StyleCop issues immediately.

---

## References

- **Spike Test**: `tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/MarteniPatchPrivateSetterValidationTest.cs`
- **Data Model**: `specs/002-immutable-readmodels/data-model.md`
- **Marten Docs**: https://martendb.io/documents/querying/patching.html
- **C# Object Initializers**: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/object-and-collection-initializers
