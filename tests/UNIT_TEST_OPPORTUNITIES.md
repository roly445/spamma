# Additional Unit Tests Opportunity Analysis

## Current Test Coverage (150 tests)

✅ **Domain Layer** (28 tests)
- Campaign & Email aggregates with edge cases
- Interaction tests between aggregates

✅ **Command Handlers** (7 tests)
- Campaign command handlers (Record, Delete)
- Email command handlers (Delete, Toggle, Receive)

✅ **Validators** (11 tests)
- Command validation rules
- Basic positive and negative cases

✅ **Query Processors** (5 tests)
- Placeholder/structural tests only

✅ **Authorization Requirements** (6 tests)
- Basic requirement instantiation

✅ **Builders** (12 tests)
- Test data factory infrastructure

✅ **Read Models** (10 tests)
- EmailLookup and CampaignSample models

---

## High-Priority Missing Test Areas

### 1. **Query Authorizers** (CRITICAL - Security)
**Not tested:** 5 query authorizers
- `GetCampaignsQueryAuthorizer`
- `GetCampaignDetailQueryAuthorizer`
- `GetEmailByIdQueryAuthorizer`
- `SearchEmailsQueryAuthorizer`
- `GetEmailMimeMessageByIdQueryAuthorizer`

**Test Pattern:** Each should verify:
- ✅ Valid requests pass authorization
- ❌ Unauthorized users blocked
- ❌ Invalid domains blocked
- ❌ Deleted campaigns blocked
- HttpContext mocking with user claims

**Estimated Tests:** 15-20 tests (3-4 per authorizer)

---

### 2. **Command Authorizers** (CRITICAL - Security)
**Not tested:** 4 command authorizers
- `RecordCampaignCaptureCommandAuthorizer`
- `DeleteCampaignCommandAuthorizer`
- `DeleteEmailCommandAuthorizer`
- `ToggleEmailFavoriteCommandAuthorizer`

**Test Pattern:** Each should verify:
- ✅ User with access succeeds
- ❌ User without access blocked
- ❌ Non-existent domains blocked
- ❌ Deleted resources blocked
- HttpContext + claim-based access control

**Estimated Tests:** 12-16 tests (3-4 per authorizer)

---

### 3. **Integration Event Subscribers** (Data Consistency)
**Not tested:** 1 subscriber
- `DeleteFileWhenEmailIsDeleted` (ICapSubscribe)
  - Listens for EmailDeleted integration event
  - Calls IMessageStoreProvider to delete MIME content

**Test Pattern:**
- ✅ EmailDeleted event triggers file deletion
- ✅ Missing file handled gracefully
- ❌ Deletion failure logged/handled
- ✅ Correct message ID passed to provider
- ✅ Multiple deletions work correctly

**Estimated Tests:** 5-7 tests

---

### 4. **Query Processor Integration** (Data Access)
**Current:** 5 placeholder tests only

**Not tested - Real Marten queries:**
- `GetCampaignDetailQueryProcessor`
  - Query CampaignSummary by ID
  - Filter by domain/user access
  - Verify projection data accuracy

- `GetCampaignsQueryProcessor`
  - List campaigns with pagination
  - Filter by subdomain
  - Sort order validation

- `GetEmailByIdQueryProcessor`
  - Load EmailLookup by ID
  - Verify all email addresses populated
  - Check deleted email handling

- `SearchEmailsQueryProcessor`
  - Full-text search by subject/sender
  - Filter by date range
  - Pagination handling
  - Campaign filtering
  - Favorite filtering

- `GetEmailMimeMessageByIdQueryProcessor`
  - Load MIME content from storage
  - Verify message deserialization
  - Handle missing files
  - Large email handling

**Test Pattern:** Each should verify:
- ✅ Valid queries return correct data
- ✅ Filters applied correctly
- ✅ Pagination works
- ❌ Non-existent IDs return nothing
- ✅ Deleted items excluded
- Performance reasonable

**Estimated Tests:** 25-35 tests (5-7 per processor)

---

### 5. **Command Handler Completeness** (Orchestration)
**Current:** Only happy paths + 1-2 error cases

**Missing for all 5 handlers:**
- Exception handling tests (save fails, repo exception)
- Marten document session failures
- Concurrent modification handling
- Large data handling (1000+ campaigns, large email)

**Per Handler Examples:**
- `RecordCampaignCaptureCommandHandler`
  - ❌ Save to Marten fails → should return error
  - ✅ Campaign not found → NotFound error
  - ✅ Campaign deleted → error code
  - ✅ Integration event publishing fails

- `DeleteEmailCommandHandler`
  - ❌ Delete aggregate fails (already deleted)
  - ❌ Save fails
  - ✅ Email not found
  - ✅ Trigger message store provider call

- `ReceivedEmailCommandHandler`
  - ✅ Email created and saved
  - ✅ Parse email addresses
  - ✅ Handle missing fields
  - ❌ Save fails
  - ✅ Subdomain validation (via query)

**Estimated Tests:** 12-15 additional tests

---

### 6. **Validator Edge Cases** (Input Validation)
**Current:** 11 tests - mostly basic

**Missing scenarios:**
- Empty strings vs null values
- Whitespace-only strings
- SQL injection attempts (if text fields)
- Very long strings (max length boundaries)
- Special characters in campaign values
- Invalid email address formats
- Null collection handling

**Per Validator:**
- `RecordCampaignCaptureValidator` - Subdomain ID, Campaign Value validation
- `DeleteCampaignValidator` - ID format validation
- `DeleteEmailCommandValidator` - ID format validation
- `ToggleEmailFavoriteCommandValidator` - ID format validation
- `ReceivedEmailCommandValidator` - Email addresses, domain validation

**Estimated Tests:** 15-20 tests

---

### 7. **Authorization Requirement Handlers** (Security Logic)
**Current:** 6 tests - only basic instantiation

**Missing - Need to test the actual handler logic:**

- `MustHaveAccessToCampaignRequirement.Handler`
  - ✅ User has access to campaign
  - ❌ User denied access to campaign
  - ❌ Campaign not found
  - ✅ Cache behavior

- `MustHaveAccessToSubdomainViaEmailRequirement.Handler`
  - ✅ User has access to email's subdomain
  - ❌ User denied access
  - ❌ Email not found
  - ❌ Subdomain not found

- `MustHaveAccessToAtLeastOneCampaignRequirement.Handler`
  - ✅ User has 1+ campaigns
  - ❌ User has 0 campaigns
  - ✅ Deleted campaigns excluded

- `MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement.Handler`
  - ✅ User has 1+ subdomains
  - ❌ User has 0 subdomains
  - ✅ Inactive subdomains excluded

**Test Pattern:** Requires:
- HttpContext mock with user claims (UserId)
- Marten IDocumentSession mock
- Query results for access validation
- Strict mock behavior

**Estimated Tests:** 16-20 tests

---

### 8. **Repository Contracts** (Data Persistence)
**Note:** Interface only, but can test implementations

**Tests for:** `IEmailRepository`, `ICampaignRepository`
- Save aggregate and verify event sourcing
- Load by ID and verify state
- Handle concurrent modifications
- Transaction rollback scenarios

**Estimated Tests:** 8-12 tests

---

### 9. **End-to-End Workflow Scenarios** (Integration)
**Not tested:** Multi-step flows

**Examples:**
1. **Email Campaign Workflow**
   - Create campaign
   - Receive emails
   - Capture emails to campaign
   - Delete campaign
   - Verify cleanup

2. **Email Management Workflow**
   - Receive email
   - Mark favorite
   - Capture to campaign
   - Delete campaign (should fail or handle gracefully)

3. **Access Control Workflow**
   - User A receives email
   - User B tries to access → blocked
   - User A can access
   - Admin can access

**Estimated Tests:** 8-12 tests

---

### 10. **Error Scenarios & Edge Cases** (Robustness)
**Missing:**
- Null reference handling
- Collection enumeration errors
- DateTime edge cases (midnight, DST transitions)
- Guid.Empty handling
- Empty collection handling
- Concurrent operations
- Resource cleanup on failure

**Estimated Tests:** 10-15 tests

---

## Test Count Projection

**Current:** 150 tests

**Recommended additions (prioritized):**

| Priority | Area | Tests | Total |
|----------|------|-------|-------|
| 🔴 **CRITICAL** | Query/Command Authorizers | 32 | 182 |
| 🔴 **CRITICAL** | Authorization Handler Logic | 18 | 200 |
| 🟠 **HIGH** | Query Processor Integration | 30 | 230 |
| 🟠 **HIGH** | Integration Event Subscribers | 6 | 236 |
| 🟡 **MEDIUM** | Command Handler Edge Cases | 12 | 248 |
| 🟡 **MEDIUM** | Validator Edge Cases | 15 | 263 |
| 🟢 **LOW** | E2E Workflows | 10 | 273 |
| 🟢 **LOW** | Robustness/Edge Cases | 12 | 285 |

**Total Potential:** ~285 tests (3.8x from current)

---

## Quick Wins (Easy to Add)

1. **Authorization Authorizer Tests** - Copy pattern from existing handlers
2. **Integration Event Subscriber Test** - Mock IMessageStoreProvider, verify call
3. **More Validator Edge Cases** - Boundary value testing
4. **E2E Workflow Tests** - Combine existing components

---

## Implementation Recommendations

**Start with (high impact, moderate effort):**
1. Query/Command Authorizers (security-critical)
2. Authorization requirement handlers (security-critical)
3. Query processor integration tests (data access verification)

**Then continue with:**
4. Command handler edge cases
5. Integration event subscribers
6. Validator edge cases

**Nice to have:**
7. E2E workflow tests
8. Robustness edge cases
