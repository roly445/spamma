# Project Context

- **Owner:** Andrew Davis
- **Project:** Spamma — modular .NET 9 email capture / SMTP testing tool
- **Stack:** .NET 9, Blazor WebAssembly, Marten (PostgreSQL event store), MediatR (CQRS), FluentValidation, CAP (Redis), Tailwind CSS v4, TypeScript/webpack, Docker Compose
- **Created:** 2026-04-21

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Testing pattern: verification-based — use `ShouldBeOk()`, `ShouldBeFailed()`, `ShouldHaveRaisedEvent<T>()` helpers from `tests/Spamma.Tests.Common/Verification/`
- Handler tests use `Mock<T>(MockBehavior.Strict)` — unmocked calls are test failures
- `Maybe.From(value)` for Some / `Maybe<T>.Nothing` for None in mock setups
- `StubTimeProvider` in `tests/Spamma.Modules.UserManagement.Tests/Fixtures/` for deterministic time
- Pass `Array.Empty<IValidator<T>>()` to handlers in tests — validation is a separate concern
- Add `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` when mocking internal interfaces with Moq
- Test structure: `Domain/` (aggregate tests), `Application/CommandHandlers/` (handler tests), `Builders/` (test data)
- SMTP test strategy documented in project instructions — SpammaMessageStore has 8-10 test scenarios, LocalMessageStoreProvider has 4-5
- Run tests: `dotnet test tests/ --no-restore`
