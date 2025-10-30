# Spamma Custom Roslyn Analyzers

Custom diagnostic analyzers for the Spamma CQRS application to enforce architecture patterns at build time.

## Rules

### SPAMMA001: Query Must Have Validator

**Severity**: Error (enforced at build time)

**Rule**: Every `IQuery<T>` must have at least one corresponding `IValidator<T>` implementation.

**Purpose**: Ensures that all CQRS queries have explicit validation logic defined before query handlers execute.

**Example - Violation**:

```csharp
// ❌ FAILS - No validator for this query
public record GetUserQuery(Guid UserId) : IQuery<GetUserQueryResult>;

// Query handler will fail if validator is missing
public class GetUserQueryHandler : QueryProcessor<GetUserQuery, GetUserQueryResult> { }
```

**Example - Compliance**:

```csharp
// ✅ PASSES - Query has corresponding validator
public record GetUserQuery(Guid UserId) : IQuery<GetUserQueryResult>;

// Validator must exist somewhere in the compilation
public class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("UserId cannot be empty");
    }
}
```

## Installation

Add reference to analyzer project in your csproj:

```xml
<ItemGroup>
    <ProjectReference Include="../../src/Spamma.Analyzers/Spamma.Analyzers.csproj" />
</ItemGroup>
```

## Configuration

Configure severity in `.editorconfig`:

```editorconfig
# Treat as error (default)
dotnet_diagnostic.SPAMMA001.severity = error

# Or as warning
dotnet_diagnostic.SPAMMA001.severity = warning

# Or silent (disable)
dotnet_diagnostic.SPAMMA001.severity = silent
```

## Future Rules

- **SPAMMA002**: Command Must Have Validator
- **SPAMMA003**: Module Isolation (prevent cross-module internal references)
- **SPAMMA004**: Aggregate ID Type Safety
- **SPAMMA005**: CQRS Attribute Validation
