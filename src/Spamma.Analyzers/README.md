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

### SPAMMA002: Command Should Have Authorizer

**Severity**: Warning

**Rule**: Every CQRS command decorated with `[BluQubeCommand]` should have at least one corresponding `AbstractRequestAuthorizer<T>` implementation.

**Purpose**: Ensures explicit authorization logic for CQRS commands. Commands should define who can execute them before handlers run.

**Example - Violation**:

```csharp
// ❌ WARNING - No authorizer for this command
[BluQubeCommand(Path = "api/users/update-profile")]
public record UpdateUserProfileCommand(Guid UserId, string Name) : ICommand;

// Command handler will execute without explicit authorization checks
public class UpdateUserProfileCommandHandler : CommandHandler<UpdateUserProfileCommand> { }
```

**Example - Compliance**:

```csharp
// ✅ PASSES - Command has corresponding authorizer
[BluQubeCommand(Path = "api/users/update-profile")]
public record UpdateUserProfileCommand(Guid UserId, string Name) : ICommand;

// Authorizer ensures only the user themselves can update their profile
public class UpdateUserProfileAuthorizer : AbstractRequestAuthorizer<UpdateUserProfileCommand>
{
    public override void BuildPolicy(UpdateUserProfileCommand request)
    {
        UseRequirement(new MustBeAuthenticatedRequirement());
        UseRequirement(new MustBeOwnerRequirement(request.UserId));
    }
}
```

### SPAMMA003: Query Should Have Authorizer

**Severity**: Warning

**Rule**: Every CQRS query decorated with `[BluQubeQuery]` should have at least one corresponding `AbstractRequestAuthorizer<T>` implementation.

**Purpose**: Ensures explicit authorization logic for CQRS queries. Queries should define who can execute them before handlers run.

**Example - Violation**:

```csharp
// ❌ WARNING - No authorizer for this query
[BluQubeQuery(Path = "api/users/profile")]
public record GetUserProfileQuery(Guid UserId) : IQuery<GetUserProfileQueryResult>;

// Query handler will execute without explicit authorization checks
public class GetUserProfileQueryHandler : QueryProcessor<GetUserProfileQuery, GetUserProfileQueryResult> { }
```

**Example - Compliance**:

```csharp
// ✅ PASSES - Query has corresponding authorizer
[BluQubeQuery(Path = "api/users/profile")]
public record GetUserProfileQuery(Guid UserId) : IQuery<GetUserProfileQueryResult>;

// Authorizer ensures proper access control
public class GetUserProfileQueryAuthorizer : AbstractRequestAuthorizer<GetUserProfileQuery>
{
    public override void BuildPolicy(GetUserProfileQuery request)
    {
        UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
```

## Installation

Add reference to analyzer project in your csproj:

```xml
<ItemGroup>
    <ProjectReference Include="../../src/Spamma.Analyzers/Spamma.Analyzers.csproj" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false"/>
</ItemGroup>
```

## Configuration

Configure severity in `.editorconfig`:

```editorconfig
# SPAMMA001 - Command Must Have Validator (default: error)
dotnet_diagnostic.SPAMMA001.severity = error

# SPAMMA002 - Command Should Have Authorizer (default: warning)
dotnet_diagnostic.SPAMMA002.severity = warning

# SPAMMA003 - Query Should Have Authorizer (default: warning)
dotnet_diagnostic.SPAMMA003.severity = warning

# Or disable specific rules
dotnet_diagnostic.SPAMMA002.severity = silent
dotnet_diagnostic.SPAMMA003.severity = silent
```

## Future Rules

- **SPAMMA004**: Module Isolation (prevent cross-module internal references)
- **SPAMMA005**: Aggregate ID Type Safety
