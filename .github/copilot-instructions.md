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

**Frontend Assets**:
- Located in `src/Spamma.App/Spamma.App/Assets/`
- **Styles**: SCSS files compiled by webpack + Tailwind CSS v4
- **Scripts**: TypeScript files (app.ts, setup wizards) compiled to JavaScript
- **Images**: Static assets (logos, icons) copied to wwwroot during build
- Build output goes to `wwwroot/` directory, served by Blazor WebAssembly
- Use `npm run build` to compile assets (or `npm run watch` for development)

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

## Implementation Notes

### SMTP & Email Capture
- **SMTP Server**: Runs on port 1025 by default (configurable)
- **Email capture**: `Spamma.Modules.EmailInbox` handles incoming SMTP connections
- **Marten event store**: All email metadata stored as events
- **Direct SMTP**: Applications connect directly to port 1025
- **MX Records**: Domain configured in UI, emails to that domain routed to Spamma

### User Authentication
- **No passwords**: Users authenticate via magic links sent to their email
- **Magic links**: `Spamma.Modules.UserManagement` generates time-limited tokens
- **Email required**: Must have working email service (SMTP) to send magic links
- **Session tokens**: After clicking link, JWT tokens issued for API access
- **Future**: Passkey/WebAuthn support planned for passwordless auth

### Initial Configuration & Setup Wizards
- **Frontend entry point**: Statically rendered Razor pages under `src/Spamma.App/Spamma.App/Components/Pages/Setup/`
- **Setup pages** (all static HTML/CSS with TypeScript interactivity):
  - `Welcome.razor` - Initial setup introduction and overview
  - `Keys.razor` - Generate security keys (with entropy collection via mouse movement)
  - `Email.razor` - Configure SMTP settings (includes provider presets: localhost, Gmail, SendGrid, Mailgun)
  - `Admin.razor` - Create initial admin user
  - `Hosting.razor` - Configure hosting details
  - `Complete.razor` - Setup completion confirmation
  - `Login.razor` - Authentication page
- **Attribute**: `[ExcludeFromInteractiveRouting]` marks pages as static (not interactive Blazor components)
- **Layout**: Uses `SetupLayout` for consistent styling
- **TypeScript interaction**: Setup scripts in `Assets/Scripts/setup-*.ts` handle UI logic (preset selection, entropy collection, form submission)
- **Configuration persistence**: Form data submitted via HTTP POST to backend commands
- **Storage**: Commands persist configuration to Marten event store via respective modules (UserManagement, DomainManagement, EmailInbox)

### Setup Mode Detection & Authorization
**Setup mode is controlled by three key services:**

- **SetupDetectionService** (`Infrastructure/Services/SetupDetectionService.cs`):
  - Checks if `SetupSettings.Completed` has a value
  - If null/not set, setup mode is enabled
  - Caches result for performance
  - Enables/disables setup mode based on configuration

- **InMemorySetupAuthService** (`Infrastructure/Services/InMemorySetupAuthService.cs`):
  - Generates random setup password on application startup
  - Logs password prominently when setup mode enabled (CRITICAL level)
  - Validates setup access attempts
  - Disables setup mode when wizard completes
  - Password format: Two random words + hyphen (e.g., "QuickTiger-123")

- **SetupModeMiddleware** (`Infrastructure/Middleware/SetupModeMiddleware.cs`):
  - Routes all requests through setup authentication
  - Allows static assets (CSS, JS, images) regardless of setup mode
  - If setup mode enabled: redirects non-setup paths to `/setup-login`
  - If setup mode disabled: blocks all `/setup` paths
  - Requires session authentication for access to setup pages

**Setup Flow:**
1. Application starts → SetupDetectionService checks configuration
2. If not configured → InMemorySetupAuthService generates random password (logged to console)
3. User accesses app → SetupModeMiddleware redirects to `/setup-login`
4. User enters setup password → Session marked as authenticated
5. User completes setup wizard → Complete.razor disables setup mode
6. Subsequent starts → Setup mode disabled, normal app flow

### Key File Locations
- **Program.cs**: `src/Spamma.App/Spamma.App/Program.cs` - Main entry point, module registration
- **Module registration**: Each module's `Module.cs` class with extension methods
- **Common error codes**: `src/modules/Spamma.Modules.Common/Application/CommonErrorCodes.cs`
- **Integration events**: `src/modules/Spamma.Modules.Common/IntegrationEvents/` directory
- **Docker setup**: `docker-compose.yml` at repository root
- **Tests**: `tests/` directory with module-specific test projects
- **CI/CD workflows**: `.github/workflows/` directory

### Database & Event Store
- **Marten**: PostgreSQL-based event sourcing framework
- **Connection string**: Configured in `appsettings.json`, defaults to Docker Compose PostgreSQL
- **Event tables**: Marten auto-creates event and snapshot tables
- **Projections**: Each module registers projections in `Infrastructure/Projections/`
- **Event stream**: All domain events persisted, replayed for state reconstruction
- **Query performance**: Projections generate read models for efficient queries

### Common Tasks

**Add a new feature to a module:**
1. Create domain aggregate method (Domain/)
2. Create command handler (Application/CommandHandlers/)
3. Add FluentValidation rules (Application/Validators/)
4. Create integration event if cross-module (Spamma.Modules.Common.IntegrationEvents)
5. Add unit tests for domain logic
6. Add handler tests with mocking

**Add a new query/API endpoint:**
1. Create query class (Application/Queries/)
2. Create query processor (Application/QueryProcessors/)
3. Add query validation (Application/Validators/)
4. Create API controller endpoint
5. Add integration tests

**Cross-module communication:**
1. Define integration event in `Spamma.Modules.Common.IntegrationEvents`
2. Publish via `IIntegrationEventPublisher.PublishAsync()`
3. Subscribe handler with `[CapSubscribe("EventName")]`
4. CAP framework handles message routing via Redis

### Debugging Tips
- **Docker logs**: `docker-compose logs -f <service-name>` to view service logs
- **PostgreSQL**: Connect with `psql postgresql://postgres:password@localhost:5432/spamma`
- **Redis**: Use `redis-cli` or watch events with `redis-cli MONITOR`
- **Webpack errors**: Clear cache: `rm -rf node_modules && npm ci && npm run build`
- **Port conflicts**: Verify ports 1025, 5432, 6379, 7181 are available
- **Test failures**: Check test logs for `AssertionFailedError` or verification failures
- **Event sourcing issues**: Verify projections by querying event tables directly

### Performance Considerations
- **Event store growth**: Monitor PostgreSQL disk usage, consider event archival for old emails
- **Projection performance**: Keep projection logic simple, avoid loading entire event history
- **Redis memory**: Set appropriate eviction policies for CAP message queues
- **Email ingestion**: SMTP server can handle concurrent connections, monitor CPU/memory
- **Blazor WebAssembly**: Large assets may impact initial load time, consider gzip compression

### Security Implementation Notes
- **Magic link tokens**: Generated with random GUID, time-limited (check `StartAuthenticationCommand`)
- **JWT tokens**: Issued after magic link validation, used for API authentication
- **Role-based access**: Each module can define custom authorization policies
- **Password hashing**: Not applicable (no passwords), but future passkey implementation will use Web Crypto API
- **HTTPS**: Required for production, self-signed certs for development
- **SMTP security**: Currently unencrypted, runs on private network or behind firewall in production