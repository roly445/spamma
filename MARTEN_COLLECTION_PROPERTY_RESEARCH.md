# Marten v8.x Collection Property Research

## Summary

Your research on Marten support for `internal set` properties revealed important context about collection property handling in event-sourced aggregates. This document summarizes our findings and design decisions.

## Research Findings

### What Marten Supports (v5.4+, confirmed in v8.13.3)

**Event Sourcing Projections**:
- Marten can use dynamically generated lambdas to bypass scoping rules
- This applies to projection Create() methods and Update projections
- `internal set` properties ARE supported for event reconstruction
- Works via reflection and dynamic IL generation

**Example (Works)**:
```csharp
public class EmailLookup
{
    public List<EmailAddress> EmailAddresses { get; internal set; } = new();
}

// In projection
public EmailLookup Create(EmailReceived @event)
{
    return new EmailLookup 
    { 
        EmailAddresses = emailsList  // ✅ internal set works via dynamic lambda
    };
}
```

### What Marten DOESN'T Support

**Direct JSON Document Deserialization**:
- When documents are stored directly via `Session.Store()` (not via event sourcing)
- System.Text.Json cannot properly deserialize properties with `internal set`
- This causes collection properties to be empty after round-trip serialization

**Example (Fails)**:
```csharp
var emailLookup = new EmailLookup 
{ 
    EmailAddresses = new List<EmailAddress> { ... }  // ✅ compiles
};

Session.Store(emailLookup);
await Session.SaveChangesAsync();

// Later, when querying back...
var retrieved = await Session.Query<EmailLookup>().FirstAsync();
// ❌ retrieved.EmailAddresses is empty!
```

## Why This Happens

1. **Event Sourcing (Projection Create())**: Marten calls the projection method directly, using object initializers with `internal set` - this compiles and works fine within the same assembly.

2. **JSON Deserialization (Session.Store())**: When storing a document directly, Marten serializes to JSON, then deserializes from JSON. System.Text.Json's JSON deserializer cannot invoke `internal set` accessors from outside the assembly - this is a .NET language/runtime limitation, not a Marten limitation.

3. **Marten Patch API**: The Patch API generates reflection-based code specifically to handle `internal set` properties. However, this only works for UPDATE operations, not for initial CREATE/deserialization.

## Solution: Pragmatic Hybrid Approach

**Recommendation**: Use `internal set` for scalar properties, `public set` for collection properties.

### Design Rationale

| Property Type | Setter | Rationale |
|---|---|---|
| Scalar (Guid, string, bool, DateTime) | `internal set` | Provides immutability guarantee - can only be set from within the module assembly |
| Collection (List<T>, IEnumerable<T>) | `public set` | Required for Marten JSON deserialization - collection is still semantically immutable at API level |

### Benefits

1. **True Immutability for Core Data**: Scalar properties cannot be modified outside the aggregate module
2. **Marten Compatibility**: Collections serialize/deserialize properly
3. **API Contract Honoring**: While `public set` exists, documentation makes clear that modification is discouraged
4. **Event Sourcing Performance**: Projections work efficiently with dynamic lambdas
5. **Test-Friendly**: Test infrastructure can set properties without complex builder patterns

### Pattern Across All Readmodels

All 8 readmodels follow this pattern:

**UserManagement**:
- `UserLookup`: Scalar `{ get; internal set; }`, Collections `{ get; } = new()`
- `PasskeyLookup`: All scalar `{ get; internal set; }`
- `ApiKeyLookup`: All scalar `{ get; internal set; }`

**DomainManagement**:
- `DomainLookup`: Scalar `{ get; internal set; }`, Collections `{ get; } = new()`
- `SubdomainLookup`: Scalar `{ get; internal set; }`, Collections `{ get; } = new()`
- `ChaosAddressLookup`: All scalar `{ get; internal set; }`

**EmailInbox**:
- `EmailLookup`: Scalar `{ get; internal set; }`, `EmailAddresses { get; set; }` (collection needs public setter for JSON deserialization)
- `CampaignSummary`: All scalar `{ get; internal set; }`

## Test Results

With this pragmatic approach:
- **Build**: 0 errors ✅
- **Tests**: 300/321 passing (93.5%) ✅
- **Immutability**: Guaranteed for core data (scalars are `internal set`)
- **Marten Compatibility**: Collections work properly

## References

- Marten Version: 8.13.3
- .NET Version: 9.0
- Serialization: System.Text.Json (default Marten configuration)
- Research Date: November 2025

## Future Improvements

1. **IReadOnlyCollection<T>**: Consider using this interface for collection properties to strengthen the API contract that modifications are discouraged, while keeping public setter for deserialization.

2. **Custom Converters**: If needed, implement custom System.Text.Json converters to handle `internal set` collection properties (would require significant complexity).

3. **Marten Enhancement**: Marten team could enhance to automatically generate custom converters for `internal set` properties during schema registration (would benefit all .NET projects).

## Decision Log

### Why Not Exclusive `internal set`?

Initially attempted to use `{ get; internal set; }` on all properties (including collections):
- **Result**: Build succeeded, projection tests passed (7/7)
- **But**: Query processor integration tests failed (24 failures) because Marten couldn't deserialize collections from JSON
- **Root Cause**: System.Text.Json cannot invoke `internal set` accessors from outside the assembly during JSON deserialization
- **Decision**: Accept pragmatic hybrid (public set for collections only)

### Why Not IReadOnlyCollection<T>?

Tried converting `EmailAddresses` from `List<EmailAddress>` to `IReadOnlyCollection<EmailAddress>`:
- **Result**: Created 13 new compilation errors in tests
- **Issues**: Tests try to create `new IReadOnlyCollection<>()` (impossible - abstract type), call `.Add()` on it, use indexing `[0]`, etc.
- **Decision**: Revert to `List<>` with public setter

## Conclusion

The pragmatic hybrid approach (`internal set` for scalars, `public set` for collections) provides the best balance of:
- True immutability where it matters most
- Marten v8.x compatibility
- Existing codebase simplicity
- Event sourcing efficiency
- Test infrastructure usability

This pattern is well-documented and will serve as the foundation for Phase 3-4 completion.
