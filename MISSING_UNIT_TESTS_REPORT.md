# Missing Unit Tests Report - Spamma.Modules.EmailInbox

## Executive Summary
The EmailInbox module has **14 existing test files** but is **missing tests for critical components**. Key gaps include:
- **Campaign command handlers** (1 of 2 tested)
- **All query processors** (0 of 5 tested)
- **Query/Command authorizers** (0 of 7 tested)
- **Critical services** (SMTP, background jobs)
- **Integration event handlers**
- **Repositories**
- **Domain aggregates** (partial coverage - Campaign not tested)

**Estimated Missing Tests: 30-45 test cases**

---

## Current Test Coverage

### ✅ Existing Tests (14 files)

#### Domain Layer (2 tests)
- `EmailAggregateTests.cs` - Email domain aggregate tests
- `EmailFavoriteTests.cs` - Email favorite functionality

#### Application Layer - Command Handlers (3 tests)
- `ReceivedEmailCommandHandlerTests.cs` - Email reception handler
- `DeleteEmailCommandHandlerTests.cs` - Email deletion handler
- `ToggleEmailFavoriteCommandHandlerTests.cs` - Email favorite toggle

#### Application Layer - Validators (2 tests)
- `DeleteEmailCommandValidatorTests.cs` - Email deletion validation
- `ReceivedEmailCommandValidatorTests.cs` - Email reception validation

#### Infrastructure - Services (5 tests)
- `SmtpHostedServiceTests.cs` - SMTP service lifecycle
- `SpammaMessageStoreTests.cs` - Email message storage
- `LocalMessageStoreProviderTests.cs` - Local message storage provider
- `SmtpCertificateServiceTests.cs` - SMTP certificate management
- `CertesLetsEncryptServiceTests.cs` - Let's Encrypt certificate service

#### Infrastructure - Projections (1 test)
- `EmailLookupProjectionTests.cs` - Email read model projection

#### Infrastructure - Integration (1 test)
- `Contract/` - Integration test placeholder

---

## Missing Tests - By Category

### 1. ❌ Campaign Command Handlers (CRITICAL - 2 handlers)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Application/CommandHandlers/Campaign/`

| Handler | Status | Severity | Notes |
|---------|--------|----------|-------|
| `RecordCampaignCaptureCommandHandler.cs` | ❌ Missing | **CRITICAL** | Complex logic with semaphore locking, campaign creation/capture, Result handling |
| `DeleteCampaignCommandHandler.cs` | ❌ Missing | **HIGH** | Campaign soft-delete, repository persistence, error handling |

**Why Important**:
- `RecordCampaignCaptureCommandHandler` contains complex concurrent logic with semaphore locks
- Needs tests for campaign creation, capture recording, thread safety
- Should verify Result<T> monad handling and domain error cases

**Suggested Tests**:
```
RecordCampaignCaptureCommandHandlerTests.cs (8-10 tests)
  - Create new campaign successfully
  - Record capture on existing campaign
  - Semaphore lock prevents race conditions
  - Invalid campaign data returns error
  - Repository save failure returns error
  - Multiple concurrent captures handled correctly

DeleteCampaignCommandHandlerTests.cs (4-5 tests)
  - Delete existing campaign successfully
  - Campaign not found returns NotFound error
  - Repository save failure returns error
  - Soft-delete sets DeletedAt timestamp correctly
```

---

### 2. ❌ Campaign Validators (2 validators)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Application/Validators/Campaign/`

| Validator | Status | Severity | 
|-----------|--------|----------|
| `DeleteCampaignValidator.cs` | ❌ Missing | HIGH |
| `RecordCampaignCaptureValidator.cs` | ❌ Missing | HIGH |

**Suggested Tests**:
```
DeleteCampaignValidatorTests.cs (3-4 tests)
  - Valid CampaignId passes
  - Invalid CampaignId (Guid.Empty) fails
  - Null values rejected

RecordCampaignCaptureValidatorTests.cs (5-6 tests)
  - Valid campaign capture data passes
  - Invalid/empty DomainId rejected
  - Invalid/empty SubdomainId rejected
  - Empty campaign value rejected
  - Campaign value length exceeds 255 characters rejected
  - Empty MessageId rejected
```

---

### 3. ❌ Query Processors (CRITICAL - 5 processors)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Application/QueryProcessors/`

| Query Processor | Status | Severity | 
|-----------------|--------|----------|
| `GetCampaignDetailQueryProcessor.cs` | ❌ Missing | **CRITICAL** |
| `GetCampaignsQueryProcessor.cs` | ❌ Missing | **CRITICAL** |
| `GetEmailByIdQueryProcessor.cs` | ❌ Missing | **CRITICAL** |
| `GetEmailMimeMessageByIdQueryProcessor.cs` | ❌ Missing | **CRITICAL** |
| `SearchEmailsQueryProcessor.cs` | ❌ Missing | **CRITICAL** |

**Why Important**:
- Query processors are the primary read layer for user-facing data
- Should verify Marten document queries, null checks, data transformation
- Critical for ensuring correct data retrieval

**Suggested Tests**:
```
GetCampaignDetailQueryProcessorTests.cs (5-6 tests)
  - Campaign found and returned with details
  - Campaign not found returns failed
  - Subdomain validation prevents unauthorized access
  - Sample message data included if available
  - Time bucket aggregation calculated correctly

GetCampaignsQueryProcessor.cs (5-6 tests)
  - Campaigns list retrieved successfully
  - Pagination parameters respected
  - Filtering by subdomain works
  - Empty result handled gracefully
  - Sorting/ordering applied correctly

GetEmailByIdQueryProcessor.cs (4-5 tests)
  - Email found and returned
  - Email not found returns failed/null
  - Email metadata correctly populated
  - Deleted emails not returned

GetEmailMimeMessageByIdQueryProcessor.cs (3-4 tests)
  - MIME message loaded from storage
  - Email not found returns failed
  - Message content correctly retrieved
  - Storage provider failure handled

SearchEmailsQueryProcessor.cs (6-8 tests)
  - Search criteria applied correctly
  - Full-text search works
  - Pagination handles large result sets
  - Filtering by campaign, subject, sender
  - Empty search results handled
  - Sort order (date, relevance) applied
```

---

### 4. ❌ Query/Command Authorizers (CRITICAL - 7 authorizers)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Application/Authorizers/`

#### Query Authorizers (5 missing)
| Authorizer | Status | Severity | 
|-----------|--------|----------|
| `GetCampaignDetailQueryAuthorizer.cs` | ❌ Missing | **CRITICAL** |
| `GetCampaignsQueryAuthorizer.cs` | ❌ Missing | **CRITICAL** |
| `GetEmailByIdQueryAuthorizer.cs` | ❌ Missing | **CRITICAL** |
| `GetEmailMimeMessageByIdQueryAuthorizer.cs` | ❌ Missing | **CRITICAL** |
| `SearchEmailsQueryAuthorizer.cs` | ❌ Missing | **CRITICAL** |

#### Command Authorizers (2 missing)
| Authorizer | Status | Severity | 
|-----------|--------|----------|
| `RecordCampaignCaptureCommandAuthorizer.cs` | ❌ Missing | **HIGH** |
| `DeleteCampaignCommandAuthorizer.cs` | ❌ Missing | **HIGH** |

**Why Important**:
- Authorization is critical for security (prevent unauthorized data access)
- Should verify user role/permission checks
- Verify subdomain/domain access restrictions

**Suggested Tests**:
```
QueryAuthorizerTests.cs (12-15 tests per authorizer)
  - Admin users (DomainManagement role) can access
  - Users with moderated subdomain access granted
  - Users with viewable subdomain access granted
  - Users with moderated domain access granted
  - Users without access denied
  - Invalid resource IDs handled
  - Suspended users denied access

CommandAuthorizerTests.cs (8-10 tests per authorizer)
  - Admin users can perform action
  - Non-admin users denied
  - Invalid campaign IDs rejected
  - Subdomain ownership verified
```

---

### 5. ❌ Authorization Requirements

**Location**: `src/modules/Spamma.Modules.EmailInbox/Application/AuthorizationRequirements/`

| Requirement | Status | Severity |
|-------------|--------|----------|
| `MustHaveAccessToCampaignRequirement.cs` | ❌ Missing | **HIGH** |

**Suggested Tests** (4-5 tests):
```
MustHaveAccessToCampaignRequirementTests.cs
  - Admin role (DomainManagement) grants access
  - Moderated subdomain access granted
  - Viewable subdomain access granted
  - Moderated domain access granted
  - User with no access denied
  - Campaign not found returns Fail
```

---

### 6. ❌ Background Jobs (3 jobs)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/BackgroundJobs/`

| Job | Status | Severity |
|-----|--------|----------|
| `BackgroundJobProcessor.cs` | ❌ Missing | MEDIUM |
| `CampaignCaptureJob.cs` | ❌ Missing | MEDIUM |
| `ChaosAddressReceivedJob.cs` | ❌ Missing | MEDIUM |

**Suggested Tests**:
```
CampaignCaptureJobTests.cs (5-6 tests)
  - Job processes campaign captures correctly
  - Batch processing handles multiple items
  - Job reschedules on failure
  - Job completes successfully

ChaosAddressReceivedJobTests.cs (4-5 tests)
  - Job processes chaos addresses
  - Notifications sent correctly
  - Job error handling
```

---

### 7. ❌ Integration Event Handlers

**Location**: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/IntegrationEventHandlers/`

| Handler | Status | Severity |
|---------|--------|----------|
| `PersistReceivedEmailHandler.cs` | ❌ Missing | **CRITICAL** |

**Why Important**:
- Handles cross-module events (e.g., domain events from DomainManagement)
- Should verify event processing, error handling, idempotency

**Suggested Tests** (5-6 tests):
```
PersistReceivedEmailHandlerTests.cs
  - Event processed successfully
  - Email persisted to database
  - Duplicate events handled (idempotency)
  - Handler errors logged correctly
  - Invalid event data rejected
```

---

### 8. ❌ Repositories (2 repositories)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Application/Repositories/`

| Repository | Status | Severity |
|-----------|--------|----------|
| `EmailRepository.cs` | ❌ Missing | MEDIUM |
| `CampaignRepository.cs` | ❌ Missing | MEDIUM |

**Suggested Tests**:
```
EmailRepositoryTests.cs (6-8 tests)
  - GetByIdAsync returns existing email
  - GetByIdAsync returns nothing for non-existent
  - SaveAsync persists new email
  - SaveAsync updates existing email
  - DeleteAsync removes email
  - Querying emails by criteria

CampaignRepositoryTests.cs (6-8 tests)
  - GetByIdAsync returns existing campaign
  - GetByIdAsync returns nothing for non-existent
  - SaveAsync persists new campaign
  - SaveAsync updates existing campaign
  - GetBySubdomainAsync filters correctly
  - Pagination handled correctly
```

---

### 9. ❌ Domain Aggregates (Partial)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Domain/`

| Aggregate | Status | Tests | Severity |
|-----------|--------|-------|----------|
| `Campaign.cs` | ❌ No tests | 0/10+ | **CRITICAL** |
| `Email.cs` | ✅ Tested | 8-10 | OK |

**Campaign Aggregate Missing Tests** (10-12 tests):
```
CampaignAggregateTests.cs
  - Create campaign with valid data succeeds
  - Create campaign with empty DomainId fails
  - Create campaign with empty SubdomainId fails
  - Create campaign with empty CampaignValue fails
  - Create campaign with null/empty MessageId fails
  - Create campaign with long CampaignValue (>255) fails
  - RecordCapture adds message to campaign
  - RecordCapture prevents duplicate captures
  - Delete campaign soft-deletes with timestamp
  - Delete campaign when already deleted fails
  - Campaign maintains message history correctly
  - Campaign state reconstructed from events correctly
```

---

### 10. ❌ Projections (Partial)

**Location**: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Projections/`

**Current**: Only `EmailLookupProjectionTests.cs` exists

**Missing Projections Tests**:
- `CampaignSummaryProjection` tests
- `ChaosAddressProjection` tests
- Other read model projections

---

## Missing Integration Tests

**Location**: `tests/Spamma.Modules.EmailInbox.Tests/Integration/`

Current: Placeholder only

**Suggested E2E/Integration Tests**:

1. **SMTP Email Reception End-to-End** (4-5 tests)
   - Email received via SMTP
   - Message stored correctly
   - Campaign created/updated
   - Projection updated
   - Integration events published

2. **Campaign Management E2E** (3-4 tests)
   - Campaign created via command
   - Campaign details retrieved via query
   - Campaign deleted via command
   - Authorization enforced throughout flow

3. **Cross-Module Integration** (3-4 tests)
   - Domain deletion cascade
   - Subdomain status changes reflected in email filtering
   - User role changes affect query authorization

4. **Error Handling & Resilience** (2-3 tests)
   - SMTP connection failures
   - Database transaction rollbacks
   - Concurrent email reception

---

## Test Metrics Summary

| Category | Existing | Missing | Priority |
|----------|----------|---------|----------|
| Command Handlers | 3/5 | **2** | **CRITICAL** |
| Validators | 2/4 | **2** | HIGH |
| Query Processors | 0/5 | **5** | **CRITICAL** |
| Query Authorizers | 0/5 | **5** | **CRITICAL** |
| Command Authorizers | 0/2 | **2** | HIGH |
| Authorization Requirements | 0/1 | **1** | HIGH |
| Domain Aggregates | 1/2 | **1** | **CRITICAL** |
| Projections | 1/3+ | **2+** | HIGH |
| Repositories | 0/2 | **2** | MEDIUM |
| Services | 5/7 | 2 | MEDIUM |
| Background Jobs | 0/3 | **3** | MEDIUM |
| Integration Event Handlers | 0/1 | **1** | **CRITICAL** |
| Integration Tests | 0/5+ | **5+** | HIGH |
| **TOTAL** | **14** | **~33-40** | |

---

## Priority Implementation Order

### Phase 1: Critical (Business Logic) - Week 1
1. Campaign Command Handlers (2 handlers, ~12 tests)
2. Query Processors (5 processors, ~28 tests)
3. Campaign Aggregate Tests (~12 tests)
4. Integration Event Handler (~5 tests)

**Estimated**: 57 tests, ~15-20 hours

### Phase 2: Security (Authorization) - Week 2
1. Query/Command Authorizers (7 authorizers, ~50 tests)
2. Authorization Requirements (~5 tests)

**Estimated**: 55 tests, ~15 hours

### Phase 3: Validation & Support - Week 3
1. Campaign Validators (2 validators, ~10 tests)
2. Repositories (2 repositories, ~14 tests)
3. Projections (2+ projections, ~10 tests)
4. Background Jobs (3 jobs, ~15 tests)

**Estimated**: 49 tests, ~12-15 hours

### Phase 4: Integration Tests - Week 4
1. SMTP Email Reception E2E (~5 tests)
2. Campaign Management E2E (~4 tests)
3. Cross-Module Integration (~4 tests)
4. Error Handling & Resilience (~3 tests)

**Estimated**: 16 tests, ~10-12 hours

---

## Testing Patterns & Resources

### Key Testing Patterns (per instructions)
- **Result Monad**: Use `.ShouldBeOk()` / `.ShouldBeFailed()` for Result verification
- **Maybe Monad**: Use `.ShouldBeSome()` / `.ShouldBeNone()` for Optional handling
- **Moq Strict**: Use `MockBehavior.Strict` to catch unmocked calls
- **Fluent Builders**: Use builders for test data setup (UserBuilder pattern)
- **No Assertions**: Verification-based testing without `Assert` keywords

### Test Infrastructure Available
- `tests/Spamma.Tests.Common/Verification/` - Result/Event verification helpers
- Mocking via Moq with Strict behavior
- StubTimeProvider for deterministic timestamps
- Builder patterns for test data

---

## Files to Create

```
tests/Spamma.Modules.EmailInbox.Tests/
  Application/
    CommandHandlers/
      Campaign/
        RecordCampaignCaptureCommandHandlerTests.cs     [NEW - 8-10 tests]
        DeleteCampaignCommandHandlerTests.cs            [NEW - 4-5 tests]
    Validators/
      Campaign/
        DeleteCampaignValidatorTests.cs                 [NEW - 3-4 tests]
        RecordCampaignCaptureValidatorTests.cs          [NEW - 5-6 tests]
    QueryProcessors/
      GetCampaignDetailQueryProcessorTests.cs           [NEW - 5-6 tests]
      GetCampaignsQueryProcessorTests.cs                [NEW - 5-6 tests]
      GetEmailByIdQueryProcessorTests.cs                [NEW - 4-5 tests]
      GetEmailMimeMessageByIdQueryProcessorTests.cs     [NEW - 3-4 tests]
      SearchEmailsQueryProcessorTests.cs                [NEW - 6-8 tests]
    Authorizers/
      Queries/
        GetCampaignDetailQueryAuthorizerTests.cs        [NEW - 5-6 tests]
        GetCampaignsQueryAuthorizerTests.cs             [NEW - 5-6 tests]
        GetEmailByIdQueryAuthorizerTests.cs             [NEW - 5-6 tests]
        GetEmailMimeMessageByIdQueryAuthorizerTests.cs  [NEW - 5-6 tests]
        SearchEmailsQueryAuthorizerTests.cs             [NEW - 5-6 tests]
      Commands/
        Campaign/
          RecordCampaignCaptureCommandAuthorizerTests.cs [NEW - 4-5 tests]
          DeleteCampaignCommandAuthorizerTests.cs        [NEW - 4-5 tests]
      MustHaveAccessToCampaignRequirementTests.cs       [NEW - 4-5 tests]
  Domain/
    CampaignAggregateTests.cs                           [NEW - 10-12 tests]
  Infrastructure/
    Projections/
      CampaignSummaryProjectionTests.cs                 [NEW - 5-6 tests]
      ChaosAddressProjectionTests.cs                    [NEW - 4-5 tests]
    Repositories/
      EmailRepositoryTests.cs                           [NEW - 6-8 tests]
      CampaignRepositoryTests.cs                        [NEW - 6-8 tests]
    Services/
      BackgroundJobs/
        CampaignCaptureJobTests.cs                      [NEW - 5-6 tests]
        ChaosAddressReceivedJobTests.cs                 [NEW - 4-5 tests]
      PersistReceivedEmailHandlerTests.cs               [NEW - 5-6 tests]
  Integration/
    SmtpEmailReceptionE2eTests.cs                       [NEW - 4-5 tests]
    CampaignManagementE2eTests.cs                       [NEW - 3-4 tests]
    CrossModuleIntegrationTests.cs                      [NEW - 3-4 tests]
    ErrorHandlingTests.cs                               [NEW - 2-3 tests]
  Builders/
    CampaignBuilder.cs                                  [NEW - Test data builder]
    CampaignSummaryBuilder.cs                           [NEW - Query result builder]
    EmailLookupBuilder.cs                               [NEW - Projection builder]
```

---

## Recommendations

1. **Start with Command Handlers** - Implement RecordCampaignCaptureCommandHandler and DeleteCampaignCommandHandler tests first (these are the most complex)

2. **Then Query Processors** - These are critical for the read layer and affect all user-facing features

3. **Authorization is Security-Critical** - All authorizers should be tested to prevent unauthorized access

4. **Use Builders Aggressively** - Create test data builders for Campaign, CampaignSummary, EmailLookup to maintain readable tests

5. **Parallel Execution** - After Phase 1, you can parallelize Phases 2-3 across different team members

6. **Coverage Tool** - Run `dotnet test --collect:"XPlat Code Coverage"` to measure actual coverage progress

---

## Total Effort Estimate

- **Phase 1**: ~20 hours (Business Logic)
- **Phase 2**: ~15 hours (Security/Authorization)
- **Phase 3**: ~15 hours (Validation/Support)
- **Phase 4**: ~12 hours (Integration)

**Total**: ~60-65 hours (~2 weeks for 1 developer, ~4-5 days for 2 developers)

Expected result: **33-40 new tests** bringing total from 14 to **47-54 tests** for EmailInbox module.
