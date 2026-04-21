---
name: "TDD Red/Green Cycle"
description: "Strict test-driven development: write failing tests first, then minimum implementation to pass"
domain: "testing"
confidence: "high"
source: "manual"
tools:
  - name: "powershell"
    description: "Run dotnet test and npm test commands"
    when: "Verify red/green state transitions"
---

## Context

**ALL agents MUST follow strict TDD red/green cycle for C#, Blazor, and TypeScript code.**

This is a **mandatory** workflow. Never write implementation code without a failing test to drive it.

## Rule

1. **RED**: Write a failing test FIRST. Run it — confirm it fails with a meaningful message.
2. **GREEN**: Write the MINIMUM code to make the test pass. Run again — confirm green.
3. **REFACTOR**: Clean up while keeping tests green.

**Never skip the red step.** A test that passes immediately means you're not testing the right thing.

## C# / .NET 9 (xUnit + Moq + Verification Helpers)

### Domain Aggregate Tests

**Location:** `tests/Spamma.Modules.{Module}.Tests/Domain/`

**Red step:**
```powershell
dotnet test tests/Spamma.Modules.UserManagement.Tests/ --filter "FullyQualifiedName~UserAggregateTests.SomeNewBehavior"
```

**Pattern:**
```csharp
// File: UserAggregateTests.cs (one type per file - SA1649)
using Spamma.Tests.Common.Verification;
using FluentAssertions;
using Xunit;

public class UserAggregateTests
{
    [Fact]
    public void SomeNewBehavior_WhenCondition_ShouldReturnOkAndRaiseEvent()
    {
        // Arrange
        var user = new UserBuilder()
            .WithName("John Doe")
            .WithEmail("john@example.com")
            .Build();

        // Act
        var result = user.SomeNewBehavior();

        // Verify - no traditional assertions, use verification helpers
        result.ShouldBeOk(value =>
        {
            value.Should().NotBe(Guid.Empty);
        });

        user.ShouldHaveRaisedEvent<SomeEventHappened>(evt =>
        {
            evt.UserId.Should().Be(user.Id);
        });
    }

    [Fact]
    public void SomeNewBehavior_WhenInvalidCondition_ShouldReturnFailed()
    {
        // Arrange
        var user = new UserBuilder().WithSuspendedStatus().Build();

        // Act
        var result = user.SomeNewBehavior();

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be("USER_SUSPENDED");
        });

        user.ShouldHaveNoEvents();
    }
}
```

**Verification Helpers (from `Spamma.Tests.Common.Verification`):**
- `result.ShouldBeOk()` — Verify Result<T, TError> success
- `result.ShouldBeOk(value => { ... })` — Verify Ok with custom assertions
- `result.ShouldBeFailed()` — Verify Result failure
- `result.ShouldBeFailed(error => { ... })` — Verify Failed with error assertions
- `aggregate.ShouldHaveRaisedEvent<TEvent>(evt => { ... })` — Verify event emitted
- `aggregate.ShouldHaveNoEvents()` — Verify no events raised
- `aggregate.ShouldHaveRaisedEventCount(int)` — Verify event count
- `aggregate.ShouldNotHaveRaisedEvent<TEvent>()` — Verify event not raised

### Command/Query Handler Tests

**Location:** `tests/Spamma.Modules.{Module}.Tests/Application/CommandHandlers/` or `QueryProcessors/`

**Red step:**
```powershell
dotnet test tests/ --filter "FullyQualifiedName~StartAuthenticationCommandHandlerTests.Handle_WhenValid_ShouldSucceed"
```

**Pattern:**
```csharp
// File: StartAuthenticationCommandHandlerTests.cs
using Moq;
using FluentAssertions;
using Xunit;

public class StartAuthenticationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserFound_ShouldSaveAndPublishEvent()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 10, 15, 10, 30, 00));

        var user = new UserBuilder().WithEmail("user@example.com").Build();

        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("user@example.com", CancellationToken.None))
            .ReturnsAsync(Maybe.From(user));

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<AuthenticationStartedIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        var handler = new StartAuthenticationCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<StartAuthenticationCommand>>(),  // No validators in tests
            new Mock<ILogger<StartAuthenticationCommandHandler>>().Object);

        var command = new StartAuthenticationCommand("user@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
        eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<AuthenticationStartedIntegrationEvent>(e => e.UserId == user.Id),
                CancellationToken.None),
            Times.Once);
    }
}
```

**Key Patterns:**
- **Moq with Strict behavior**: `new Mock<T>(MockBehavior.Strict)` — catches unmocked calls
- **Maybe<T> mocking**: Use `Maybe.From(value)` for Some, `Maybe<T>.Nothing` for None
- **Result mocking**: Use `Result.Ok()` or `Result.Fail(error)`
- **No validators**: Pass `Array.Empty<IValidator<T>>()` — validation is separate layer
- **StubTimeProvider**: For deterministic time (`tests/.../Fixtures/StubTimeProvider.cs`)

## Blazor WebAssembly (bUnit)

**Framework:** bUnit is NOT yet configured in `Spamma.App.Tests.csproj`.

**When adding bUnit tests:**
1. Add NuGet package: `dotnet add tests/Spamma.App.Tests/ package bUnit`
2. Follow this pattern:

**Red step:**
```powershell
dotnet test tests/Spamma.App.Tests/ --filter "FullyQualifiedName~PasskeyManagerTests"
```

**Pattern:**
```csharp
using Bunit;
using Moq;
using Xunit;

public class PasskeyManagerTests : TestContext
{
    [Fact]
    public void Render_WhenNoPasskeys_DisplaysEmptyState()
    {
        // Arrange
        var querierMock = new Mock<IQuerier>();
        querierMock
            .Setup(x => x.Send(It.IsAny<GetMyPasskeysQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<GetMyPasskeysQueryResult>
            {
                Status = QueryResultStatus.Succeeded,
                Data = new GetMyPasskeysQueryResult(Enumerable.Empty<PasskeySummary>())
            });

        Services.AddSingleton(querierMock.Object);

        // Act
        var cut = RenderComponent<PasskeyManager>();

        // Verify
        cut.Find("div.empty-state").TextContent.Should().Contain("No passkeys registered");
    }
}
```

**Key Patterns:**
- Inherit from `TestContext` for component rendering
- Mock `ICommander` and `IQuerier` dependencies
- Use `Services.AddSingleton(mock.Object)` for DI
- `RenderComponent<T>()` to render
- `Find()` and `FindAll()` for DOM queries

## TypeScript (Webpack Build)

**No test framework configured yet.**

`package.json` in `src/Spamma.App/Spamma.App/` has no test script or Jest/Vitest/Mocha packages.

**When adding TypeScript tests:**
1. Install test framework: `npm install --save-dev jest @types/jest ts-jest` (or Vitest)
2. Add test script to `package.json`: `"test": "jest"`
3. Configure `jest.config.js` or `vitest.config.ts`

**Red step (once configured):**
```powershell
cd src/Spamma.App/Spamma.App
npm test -- webauthn-utils.test.ts
```

**Pattern (Jest):**
```typescript
// File: webauthn-utils.test.ts
import { validateWebAuthnAssertion } from './webauthn-utils';

describe('validateWebAuthnAssertion', () => {
    it('should return true for valid assertion', () => {
        const assertion = { /* valid data */ };
        
        const result = validateWebAuthnAssertion(assertion);
        
        expect(result).toBe(true);
    });

    it('should return false for invalid signature', () => {
        const assertion = { /* invalid data */ };
        
        const result = validateWebAuthnAssertion(assertion);
        
        expect(result).toBe(false);
    });
});
```

## Red/Green Checklist

Before writing ANY implementation code:

- [ ] Test file created BEFORE implementation file
- [ ] Test runs and FAILS with a meaningful message (not a compile error)
- [ ] Failure message clearly describes missing behavior
- [ ] Implementation written — minimum code to pass
- [ ] Test runs and PASSES
- [ ] No implementation code exists without a test driving it

**Common anti-patterns to avoid:**
- ❌ Writing implementation first, then "adding tests later"
- ❌ Tests that pass immediately (green before red)
- ❌ Tests that fail due to compile errors (not runtime behavior)
- ❌ Over-implementation in green step (add only what's needed)
- ❌ Using traditional assertions instead of verification helpers (C# domain tests)
- ❌ Skipping Moq Strict behavior (handler tests)

## Running Tests

**C# - All tests:**
```powershell
dotnet test tests/ --no-restore
```

**C# - Specific module:**
```powershell
dotnet test tests/Spamma.Modules.UserManagement.Tests/
```

**C# - Specific test:**
```powershell
dotnet test --filter "FullyQualifiedName~UserAggregateTests.SomeTest"
```

**TypeScript (once configured):**
```powershell
cd src/Spamma.App/Spamma.App
npm test
```

## Examples

**Strong coverage (follow these):**
- `tests/Spamma.Modules.UserManagement.Tests/Domain/UserAggregateTests.cs` — 8 tests passing
- `tests/Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/` — All 12 handlers tested

**Weak coverage (needs work):**
- `tests/Spamma.Modules.DomainManagement.Tests/` — ChaosAddress handlers untested
- `tests/Spamma.Modules.EmailInbox.Tests/Application/QueryProcessors/` — Placeholder tests only
