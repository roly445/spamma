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

**Severity**: Info (informational, non-blocking)

**Rule**: Every CQRS command decorated with `[BluQubeCommand]` should have at least one corresponding `AbstractRequestAuthorizer<T>` implementation.

**Purpose**: Encourages explicit authorization logic for CQRS commands. Commands should define who can execute them before handlers run.

**Example - Violation**:

```csharp
// ⚠️ INFO - No authorizer for this command (allowed but discouraged)
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
    protected override Task<AuthorizationResult> AuthorizeAsync(
        UpdateUserProfileCommand request,
        AuthorizationHandlerContext context,
        CancellationToken cancellationToken)
    {
        // Check if current user is updating their own profile
        var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserId, out var userId) && userId == request.UserId)
        {
            return Task.FromResult(AuthorizationResult.Success());
        }

        return Task.FromResult(AuthorizationResult.Failed());
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
# SPAMMA001 - Command Must Have Validator (default: warning)
dotnet_diagnostic.SPAMMA001.severity = error

# SPAMMA002 - Command Should Have Authorizer (default: info)
dotnet_diagnostic.SPAMMA002.severity = info

# Or disable
dotnet_diagnostic.SPAMMA002.severity = silent
```

## Future Rules

- **SPAMMA003**: Module Isolation (prevent cross-module internal references)
- **SPAMMA004**: Aggregate ID Type Safety
- **SPAMMA005**: CQRS Attribute Validation
