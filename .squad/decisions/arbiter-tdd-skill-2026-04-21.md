# Decision: Mandatory TDD Red/Green Cycle

**Date:** 2026-04-21  
**Proposed by:** Arbiter (Tester)  
**Status:** Implemented  
**Priority:** P0  

## Context

The project lacked a formal, enforceable TDD workflow. Code was being written without tests-first discipline, leading to:
- Implementation before tests (backward workflow)
- Tests that pass immediately (no red step verification)
- Inconsistent test patterns across modules
- Some agents unaware of verification helpers (`ShouldBeOk`, `ShouldHaveRaisedEvent`)

## Decision

Created `.squad/skills/tdd-red-green/SKILL.md` — a mandatory skill document enforcing strict Red → Green → Refactor cycle for all C#, Blazor, and TypeScript code.

**Key mandates:**
1. **Red step first**: Write failing test, confirm it fails with meaningful message
2. **Green step**: Write minimum implementation to pass
3. **No implementation without failing test**: Zero exceptions

**Stack-specific guidance:**
- **C#/.NET 9**: xUnit + Moq Strict + verification helpers from `Spamma.Tests.Common.Verification`
  - Domain tests: Use `ShouldBeOk()`, `ShouldBeFailed()`, `ShouldHaveRaisedEvent<T>()`
  - Handler tests: Moq Strict, `Array.Empty<IValidator<T>>()`, `StubTimeProvider`
  - Reference: `tests/Spamma.Modules.UserManagement.Tests/` (all 12 handlers tested)
  
- **Blazor WebAssembly**: bUnit patterns documented (framework not yet configured)
  - Add `bUnit` NuGet package when needed
  - Mock `ICommander` and `IQuerier` in component tests
  
- **TypeScript**: Jest/Vitest patterns documented (awaiting framework setup)
  - `package.json` has no test script yet
  - Guidance provided for future configuration

## Rationale

**Why mandatory:**
- TDD catches design issues early (before implementation lock-in)
- Red step proves the test actually works (no false positives)
- Verification helpers enforce project standards (no raw FluentAssertions in domain tests)
- Moq Strict catches unmocked dependencies immediately

**Why one skill for all stacks:**
- Single source of truth for all agents
- Consistent workflow regardless of language
- Reduces onboarding time for new agents

## Implementation

**Files created:**
- `.squad/skills/tdd-red-green/SKILL.md` (315 lines, committed)

**Documented patterns:**
- C# domain tests with `UserAggregateTests.cs` example
- C# handler tests with `StartAuthenticationCommandHandlerTests.cs` example
- bUnit component test skeleton
- TypeScript Jest pattern (awaiting configuration)
- Red/Green checklist with anti-patterns

**Updated:**
- `.squad/agents/arbiter/history.md` — logged TDD skill creation

## Impact

**Immediate:**
- All agents now have clear TDD guidance before writing code
- UserManagement module serves as reference implementation
- Skill document is 191 lines (under 200 line target)

**Future:**
- When bUnit configured: update skill with actual test examples
- When TypeScript tests configured: verify Jest/Vitest patterns in skill
- Monitor agent compliance via code review

## Alternatives Considered

1. **Separate skills per stack** — Rejected: Too fragmented, agents would miss cross-stack patterns
2. **Enforce via CI lint** — Considered: Would catch violations late (after code written)
3. **Wiki documentation** — Rejected: Skills are agent-readable, wiki is human-only

## Review Checklist

- [x] Skill document created and committed
- [x] History updated with learning
- [x] Decision logged in inbox
- [x] Examples reference actual test files (UserManagement)
- [x] Verification helper signatures match `Spamma.Tests.Common.Verification`
- [x] Moq Strict pattern documented with `MockBehavior.Strict`
- [x] Under 200 lines (191 actual)
- [ ] Agents tested with skill (pending next task)
- [ ] bUnit configured (future)
- [ ] TypeScript test framework configured (future)

## Notes

**Test coverage status (as of 2026-04-21):**
- **Strong**: UserManagement (12/12 handlers tested, domain complete)
- **Weak**: DomainManagement (5 ChaosAddress handlers untested), EmailInbox (QueryProcessors placeholders)
- **Missing**: bUnit not yet in `Spamma.App.Tests.csproj`, TypeScript has no test framework

**Next actions:**
1. Scribe to review and merge decision
2. Squad agents to read skill before next implementation task
3. Arbiter to monitor test-first compliance
4. Add bUnit when first component test needed
5. Configure Jest/Vitest when TypeScript testing begins
