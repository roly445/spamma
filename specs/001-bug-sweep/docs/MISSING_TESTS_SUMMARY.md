# Missing Unit Tests - Quick Summary

## Overview

The **Spamma.Modules.EmailInbox** module currently has **14 test files** but is missing tests for **~33-40 critical components**.

Current Status: 14 existing tests | Missing: ~80-90 test cases

## Critical Gaps (Do First!)

### 1. Campaign Command Handlers (0/2 tested)
- `RecordCampaignCaptureCommandHandler` - Complex semaphore locking logic
- `DeleteCampaignCommandHandler` - Soft-delete with timestamp

**Tests needed**: 12-15 tests | **Effort**: 4-5 hours

### 2. Query Processors (0/5 tested) 
- `GetCampaignDetailQueryProcessor`
- `GetCampaignsQueryProcessor`
- `GetEmailByIdQueryProcessor`
- `GetEmailMimeMessageByIdQueryProcessor`
- `SearchEmailsQueryProcessor`

**Tests needed**: 28-32 tests | **Effort**: 8-10 hours

### 3. Query/Command Authorizers (0/7 tested)
- 5 Query authorizers (GetCampaignDetail, GetCampaigns, GetEmailById, GetEmailMimeMessageById, SearchEmails)
- 2 Command authorizers (RecordCampaignCapture, DeleteCampaign)

**Tests needed**: 50-55 tests | **Effort**: 12-15 hours

### 4. Campaign Aggregate (0/1 tested)
- `Campaign.cs` - Domain aggregate with Create, RecordCapture, Delete methods

**Tests needed**: 10-12 tests | **Effort**: 3-4 hours

### 5. Integration Event Handler (0/1 tested)
- `PersistReceivedEmailHandler` - Cross-module event processing

**Tests needed**: 5-6 tests | **Effort**: 2-3 hours

## Medium Priority

### Validators (2/4 tested)
- Missing: Campaign validators (DeleteCampaignValidator, RecordCampaignCaptureValidator)
- Tests needed: 10-12 tests | Effort: 2-3 hours

### Repositories (0/2 tested)
- EmailRepository, CampaignRepository
- Tests needed: 12-16 tests | Effort: 3-4 hours

### Background Jobs (0/3 tested)
- CampaignCaptureJob, ChaosAddressReceivedJob, BackgroundJobProcessor
- Tests needed: 12-15 tests | Effort: 3-4 hours

### Projections (1/3+ tested)
- Missing: CampaignSummaryProjection, ChaosAddressProjection
- Tests needed: 10-12 tests | Effort: 2-3 hours

## Integration Tests (0/0 tested)
- SMTP email reception end-to-end
- Campaign management end-to-end
- Cross-module integration
- Error handling & resilience

**Tests needed**: 15-20 tests | **Effort**: 4-5 hours

## Summary by Numbers

| Layer | Existing | Missing | Estimated Tests |
|-------|----------|---------|-----------------|
| **Command Handlers** | 3/5 | 2 | 12-15 |
| **Query Processors** | 0/5 | 5 | 28-32 |
| **Authorizers** | 0/7 | 7 | 50-55 |
| **Validators** | 2/4 | 2 | 10-12 |
| **Domain Aggregates** | 1/2 | 1 | 10-12 |
| **Repositories** | 0/2 | 2 | 12-16 |
| **Services** | 5/7 | 2 | 10-15 |
| **Background Jobs** | 0/3 | 3 | 12-15 |
| **Projections** | 1/3+ | 2+ | 10-12 |
| **Integration Events** | 0/1 | 1 | 5-6 |
| **Integration Tests** | 0 | 5+ | 15-20 |
| **TOTAL** | **14** | **~33** | **~175-200** |

## Recommended Implementation Plan

### Week 1: Critical Business Logic
- Campaign command handlers (4-5 hours)
- Query processors (8-10 hours)
- Campaign aggregate (3-4 hours)

### Week 2: Security
- Query authorizers (10-12 hours)
- Command authorizers (4-5 hours)

### Week 3: Support & Validation
- Repositories (3-4 hours)
- Validators (2-3 hours)
- Projections (2-3 hours)
- Background jobs (3-4 hours)

### Week 4: Integration
- Integration event handler (2-3 hours)
- End-to-end tests (4-5 hours)

## Key Test Patterns (From Copilot Instructions)

### Use Result Monad Verification
```csharp
result.ShouldBeOk(value => {
    value.CampaignId.Should().NotBe(Guid.Empty);
});

result.ShouldBeFailed();
```

### Use Moq Strict Behavior
```csharp
var mock = new Mock<IRepository>(MockBehavior.Strict);
mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Maybe.From(entity));
```

### Use Fluent Builders
```csharp
var campaign = new CampaignBuilder()
    .WithCampaignValue("test@example.com")
    .WithDomainId(Guid.NewGuid())
    .Build();
```

## Next Steps

1. Review the detailed `MISSING_UNIT_TESTS_REPORT.md` for full breakdown
2. Create Campaign test builders in `tests/Spamma.Modules.EmailInbox.Tests/Builders/`
3. Start with Campaign command handler tests (most complex logic)
4. Implement query processor tests (highest user impact)
5. Follow up with authorizers (security critical)

---

See `MISSING_UNIT_TESTS_REPORT.md` for detailed information, test suggestions, and file structure.
