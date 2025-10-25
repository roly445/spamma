# Spamma - Copilot Instructions

Spamma is a modular .NET 9 application using Blazor WebAssembly, following Clean Architecture with CQRS and event sourcing patterns.

## Architecture Overview

**Modular Monolith**: Each domain is organized as a self-contained module under `src/modules/`:
- `Spamma.Modules.UserManagement` - Authentication, user management
- `Spamma.Modules.DomainManagement` - Domain/hostname management  
- `Spamma.Modules.EmailInbox` - Email processing and inbox features
- `Spamma.Modules.Common` - Shared contracts and services

Each module follows the pattern: `Module/Module.Client` where `.Client` contains contracts and DTOs for cross-module communication.

**Technology Stack**:
- **Backend**: .NET 9, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation
- **Frontend**: Blazor WebAssembly with Tailwind CSS (via webpack)
- **Messaging**: CAP framework with Redis for integration events
- **Infrastructure**: Docker Compose with PostgreSQL, Redis, MailHog

## Key Patterns

### Module Structure
Each module contains:
```
Application/
  CommandHandlers/     - CQRS command handling
  QueryProcessors/     - Query handling
  Authorizers/         - Custom authorization logic
  Validators/          - FluentValidation rules
Domain/
  [Entity]Aggregate/   - Domain aggregates with business logic
Infrastructure/
  Projections/         - Marten event projections
  Repositories/        - Data access implementations
```

### Module Registration
All modules expose a static `Module` class with extension methods:
- `AddUserManagement()` - Register services
- `ConfigureUserManagement()` - Configure Marten projections
- `AddJsonConvertersForUserManagement()` - JSON serialization setup

### CQRS Commands/Queries
- Commands use `BluQube.Commands.CommandHandler<T>` base class
- Queries use `BluQube.Queries.QueryProcessor<TQuery, TResult>`
- All handlers include validation via `IValidator<T>` injection
- Results use `CommandResult` / `QueryResult` wrapper types

### Integration Events
Cross-module communication uses CAP integration events:
- Publish: `IIntegrationEventPublisher.PublishAsync()`
- Subscribe: `[CapSubscribe("EventName")]` attributes on handlers
- Events live in `Spamma.Modules.Common.IntegrationEvents`

## Development Workflow

### Local Development
```powershell
# Start infrastructure
docker-compose up -d

# Build and run
dotnet run --project src/Spamma.App/Spamma.App

# Frontend assets (if modified)
cd src/Spamma.App/Spamma.App
npm run build  # or npm run watch
```

### Key Configuration
- Connection strings in `appsettings.json` point to Docker services
- Database configuration loaded via `AddDatabaseConfiguration()` extension
- Module registration in `Program.cs` follows: `builder.Services.AddUserManagement().AddDomainManagement().AddEmailInbox()`

### Project References
- Main app references all `.Client` modules for contracts
- Server modules reference `Spamma.Modules.Common` for shared services
- Shared project `Spamma.Shared` contains StyleCop configuration

## Common Conventions

- **Authorization**: Use `MustBeAuthenticatedRequirement` for secured endpoints
- **Error Handling**: Return `BluQubeErrorData` with error codes from `CommonErrorCodes`
- **Entity IDs**: Use strongly-typed ID classes (e.g., `UserId`, `DomainId`)
- **Time**: Inject `TimeProvider` for testable time operations
- **Validation**: FluentValidation rules registered per module, validated in command handlers

## Testing Strategy

Spamma uses a **verification-based testing pattern** with event sourcing and the Result monad, enabling clean, expressive domain tests without assertion keywords.

### Domain Logic Tests (Unit)
**Framework**: xUnit + custom verification helpers in `tests/Spamma.Tests.Common/Verification/`

Test domain aggregates by verifying Result returns and business logic without assertions:
```csharp
// Arrange
var user = new UserBuilder()
    .WithName("John Doe")
    .WithEmail("john@example.com")
    .Build();

// Act
var result = user.StartAuthentication(DateTime.UtcNow);

// Verify - no assertions, only verification helpers
result.ShouldBeOk(authEvent =>
{
    authEvent.AuthenticationAttemptId.Should().NotBe(Guid.Empty);
});
```

**Core Verification Helpers** (`tests/Spamma.Tests.Common/Verification/`):
- `ResultAssertions` - Fluent Result<T, TError> verification:
  - `.ShouldBeOk()` - Verify Ok state
  - `.ShouldBeFailed()` - Verify Failed state
- `EventVerificationExtensions` - Aggregate event verification:
  - `.ShouldHaveRaisedEvent<TEvent>(verify)` - Verify event emitted with optional custom verification
  - `.ShouldHaveNoEvents()` - Verify no events raised
  - `.ShouldHaveRaisedEventCount(int)` - Verify event count

**Test Data Builders** (`tests/Spamma.Modules.UserManagement.Tests/Builders/`):
- `UserBuilder` - Fluent builder with sensible defaults for test setup
  - `.WithName()`, `.WithEmail()`, `.WithRole()`, etc.
  - `.Build()` - Creates configured User aggregate

### Command/Query Handler Tests (Integration)
**Framework**: xUnit + Moq for dependency mocking

Handler tests verify CQRS orchestration: repository access, domain logic execution, state persistence, and integration event publishing.

**Testing Pattern** (`tests/Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/`):
```csharp
// Setup dependencies with Moq (using Strict behavior to catch unmocked calls)
var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
var timeProvider = new StubTimeProvider(new DateTime(2024, 10, 15, 10, 30, 00));

// Create handler with empty validators array (we don't test FluentValidation)
var handler = new StartAuthenticationCommandHandler(
    repositoryMock.Object,
    timeProvider,
    eventPublisherMock.Object,
    Array.Empty<IValidator<StartAuthenticationCommand>>(),
    new Mock<ILogger<StartAuthenticationCommandHandler>>().Object);

// Arrange - mock repository to return Maybe<User>
repositoryMock
    .Setup(x => x.GetByEmailAddressAsync("user@example.com", CancellationToken.None))
    .ReturnsAsync(Maybe.From(user));

repositoryMock
    .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
    .ReturnsAsync(Result.Ok());

eventPublisherMock
    .Setup(x => x.PublishAsync(It.IsAny<AuthenticationStartedIntegrationEvent>(), CancellationToken.None))
    .Returns(Task.CompletedTask);

// Act
var result = await handler.Handle(command, CancellationToken.None);

// Verify - check repository was called correctly, event published with right data
result.Should().NotBeNull();
repositoryMock.Verify(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
eventPublisherMock.Verify(
    x => x.PublishAsync(
        It.Is<AuthenticationStartedIntegrationEvent>(e => 
            e.UserId == user.Id && e.EmailAddress == "user@example.com"),
        CancellationToken.None),
    Times.Once);
```

**Key Patterns**:
- **Moq with Strict behavior**: Catches unmocked calls to verify handlers only access expected dependencies
- **Maybe<T> mocking**: Use `Maybe.From(value)` for Some/Just, `Maybe<T>.Nothing` for None
- **Result mocking**: Use `Result.Ok()` for success, `Result.Fail()` for failure
- **StubTimeProvider**: For deterministic timestamp-based assertions (`tests/Spamma.Modules.UserManagement.Tests/Fixtures/StubTimeProvider.cs`)
- **Event verification via callback**: Capture published events to verify they contain correct data
- **No validators**: Pass `Array.Empty<IValidator<T>>()` since command validation isn't the handler's responsibility

**Test Coverage**:
- Happy path: Command succeeds, repository called, event published
- Error paths: User not found (NotFound), user suspended (error code), save fails (SavingChangesFailed)
- Idempotency: Multiple calls create different event instances/IDs

### Test Project Structure
```
tests/
  Spamma.Tests.Common/
    Verification/
      EventVerificationExtensions.cs    # Event verification fluent API
      ResultAssertions.cs               # Result monad verification
    Builders/                           # Shared test data builders (if module-agnostic)
  Spamma.Modules.UserManagement.Tests/
    Domain/
      UserAggregateTests.cs             # ✅ 8 tests passing
    Fixtures/
      StubTimeProvider.cs               # Deterministic time for testing
    Builders/
      UserBuilder.cs                    # Module-specific builders
    Application/
      CommandHandlers/
        StartAuthenticationCommandHandlerTests.cs  # ✅ 5 tests passing
```

### Key Testing Conventions
- **Verification-based**: Use custom verification helpers, NO traditional assertions
- **Result monad first**: All business logic returns Result<T, TError> - verify with `.ShouldBeOk()` / `.ShouldBeFailed()`
- **Fluent builders**: Use `UserBuilder` for readable test setup
- **Focus on behavior**: Verify state changes through Result returns and aggregate state
- **No reflection hacks**: Avoid setting private fields - rely on aggregate public methods
- **Module-specific builders**: Keep builders in test project that uses them (has internal access)
- **Internal type mocking**: Add `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` for Moq to proxy internal interfaces
- **Empty validators**: Pass `Array.Empty<IValidator<T>>()` to handlers since validation testing belongs in separate layer