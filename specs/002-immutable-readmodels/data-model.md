# Data Model Analysis: Immutable ReadModels

**Feature**: `002-immutable-readmodels` | **Date**: November 17, 2025

---

## Readmodel Inventory

### Spamma.Modules.UserManagement

#### 1. UserLookup
**File**: `src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/UserLookup.cs`

**Purpose**: Central user identity and role lookup model

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| Name | string | Auto-property | `= string.Empty` | Username/display name |
| EmailAddress | string | Auto-property | `= string.Empty` | User email |
| CreatedAt | DateTime | Auto-property | - | Creation timestamp |
| LastLoginAt | DateTime? | Auto-property | - | Nullable, updated on auth |
| LastPasskeyAuthenticationAt | DateTime? | Auto-property | - | Nullable, updated on passkey auth |
| IsSuspended | bool | Auto-property | - | Suspension status flag |
| SuspendedAt | DateTime? | Auto-property | - | Nullable suspension time |
| SystemRole | SystemRole | Auto-property | - | Enum: Admin, User, etc. |
| ModeratedDomains | List<Guid> | Auto-property | `= new()` | Collection (moderation scope) |
| ModeratedSubdomains | List<Guid> | Auto-property | `= new()` | Collection (moderation scope) |
| ViewableSubdomains | List<Guid> | Auto-property | `= new()` | Collection (visibility scope) |

**Total Properties**: 12  
**Collections**: 3 (ModeratedDomains, ModeratedSubdomains, ViewableSubdomains)  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: Multiple collection replacements in projections

---

#### 2. PasskeyProjection
**File**: `src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/PasskeyProjection.cs`

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| UserId | Guid | Auto-property | - | Foreign key to user |
| DisplayName | string | Auto-property | `= string.Empty` | Passkey friendly name |
| Algorithm | string | Auto-property | `= string.Empty` | WebAuthn algorithm |
| RegisteredAt | DateTime | Auto-property | - | Creation timestamp |
| LastUsedAt | DateTime? | Auto-property | - | Nullable last usage |
| IsRevoked | bool | Auto-property | - | Revocation status |
| RevokedAt | DateTime? | Auto-property | - | Nullable revocation time |

**Total Properties**: 8  
**Collections**: 0  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: None - straightforward conversion

---

#### 3. ApiKeyProjection
**File**: `src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/ApiKeyProjection.cs`

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| UserId | Guid | Auto-property | - | Foreign key to user |
| DisplayName | string | Auto-property | `= string.Empty` | Key friendly name |
| HashedValue | string | Auto-property | `= string.Empty` | Hashed key (for comparison) |
| ExpiresAt | DateTime? | Auto-property | - | Nullable expiration |
| CreatedAt | DateTime | Auto-property | - | Creation timestamp |

**Total Properties**: 6  
**Collections**: 0  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: None - straightforward conversion

---

### Spamma.Modules.DomainManagement

#### 4. DomainLookup
**File**: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/DomainLookup.cs`

**Purpose**: Domain/hostname configuration and status lookup

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| OwnerId | Guid | Auto-property | - | Foreign key to user |
| DomainName | string | Auto-property | `= string.Empty` | E.g., "example.com" |
| Status | DomainStatus | Auto-property | - | Enum: Active, Pending, etc. |
| CreatedAt | DateTime | Auto-property | - | Creation timestamp |
| VerifiedAt | DateTime? | Auto-property | - | Nullable verification time |
| MxRecordVerified | bool | Auto-property | - | MX record status |
| DkimPublicKey | string | Auto-property | `= string.Empty` | DKIM key for signing |
| DkimSelector | string | Auto-property | `= string.Empty` | DKIM selector |
| Subdomains | List<SubdomainLookup> | Auto-property | `= new()` | Collection (nested objects) |
| ChaosAddresses | List<ChaosAddressLookup> | Auto-property | `= new()` | Collection (nested objects) |

**Total Properties**: 11  
**Collections**: 2 (with complex object types)  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: Nested object collections (SubdomainLookup, ChaosAddressLookup) - requires Patch strategy for replacements

---

#### 5. SubdomainLookup
**File**: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/SubdomainLookup.cs`

**Purpose**: Subdomain/email handler configuration lookup

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| DomainId | Guid | Auto-property | - | Foreign key to domain |
| SubdomainName | string | Auto-property | `= string.Empty` | E.g., "*.example.com" or "mail.example.com" |
| Status | SubdomainStatus | Auto-property | - | Enum: Active, Suspended, etc. |
| CreatedAt | DateTime | Auto-property | - | Creation timestamp |
| HandlerType | string | Auto-property | `= string.Empty` | Handler identifier |

**Total Properties**: 6  
**Collections**: 0  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: None - straightforward conversion

---

#### 6. ChaosAddressLookup
**File**: `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/ChaosAddressLookup.cs`

**Purpose**: Chaos/intentional-failure email address configuration

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| DomainId | Guid | Auto-property | - | Foreign key to domain |
| SubdomainId | Guid | Auto-property | - | Foreign key to subdomain |
| LocalPart | string | Auto-property | `= string.Empty` | Email local part (before @) |
| ConfiguredSmtpCode | SmtpResponseCode | Auto-property | - | Enum: MailboxUnavailable, etc. |
| CreatedAt | DateTime | Auto-property | - | Creation timestamp |

**Total Properties**: 6  
**Collections**: 0  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: None - straightforward conversion

---

### Spamma.Modules.EmailInbox

#### 7. EmailLookup
**File**: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/ReadModels/EmailLookup.cs`

**Purpose**: Email message metadata and search index

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| SubdomainId | Guid | Auto-property | - | Foreign key to subdomain |
| From | string | Auto-property | `= string.Empty` | Sender email address |
| To | string | Auto-property | `= string.Empty` | Recipient (comma-separated) |
| Subject | string | Auto-property | `= string.Empty` | Email subject |
| ReceivedAt | DateTime | Auto-property | - | Delivery timestamp |
| IsRead | bool | Auto-property | - | Read status flag |
| IsArchived | bool | Auto-property | - | Archive status flag |
| Body | string | Auto-property | `= string.Empty` | HTML body (searchable) |

**Total Properties**: 9  
**Collections**: 0  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: None - straightforward conversion

---

#### 8. CampaignSummary
**File**: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/ReadModels/CampaignSummary.cs`

**Purpose**: Email campaign statistics and metadata

| Property | Type | Pattern | Initialize | Notes |
|----------|------|---------|-----------|-------|
| Id | Guid | Auto-property | - | Primary key |
| SubdomainId | Guid | Auto-property | - | Foreign key to subdomain |
| Name | string | Auto-property | `= string.Empty` | Campaign name |
| Description | string | Auto-property | `= string.Empty` | Campaign description |
| CreatedAt | DateTime | Auto-property | - | Creation timestamp |
| EmailCount | int | Auto-property | - | Count of emails in campaign |
| UnreadCount | int | Auto-property | - | Count of unread emails |
| LastEmailAt | DateTime? | Auto-property | - | Nullable timestamp of last email |

**Total Properties**: 8  
**Collections**: 0  
**Conversion Pattern**: All auto-properties → private setters  
**Challenges**: None - straightforward conversion

---

## Summary

| Metric | Value |
|--------|-------|
| Total Readmodels | 8 |
| Total Properties | ~80 |
| Auto-properties | ~80 (100%) |
| Collections | 5 (across 2 readmodels) |
| Readmodels requiring complex Patch handling | 1 (DomainLookup with nested collections) |
| Estimated conversion effort | Low - all straightforward property conversions |

## Conversion Strategy

### Pattern 1: Simple Properties (Most common)
All scalar properties (Guid, string, DateTime, bool, enum, int) convert identically:

**Before**:
```csharp
public string Name { get; set; } = string.Empty;
public DateTime CreatedAt { get; set; }
public bool IsActive { get; set; }
```

**After**:
```csharp
public string Name { get; private set; } = string.Empty;
public DateTime CreatedAt { get; private set; }
public bool IsActive { get; private set; }
```

### Pattern 2: Collection Properties
Collections use getter-only auto-property with default initialization:

**Before**:
```csharp
public List<Guid> ModeratedDomains { get; set; } = new();
```

**After**:
```csharp
public List<Guid> ModeratedDomains { get; } = new();
```

### Pattern 3: Nullable Properties
Nullable properties follow scalar pattern:

**Before**:
```csharp
public DateTime? LastLoginAt { get; set; }
```

**After**:
```csharp
public DateTime? LastLoginAt { get; private set; }
```

## Marten Compatibility

### Deserialization (Creating Readmodels)
- **Status**: ✅ Compatible
- **Reason**: Marten's JSON deserialization uses reflection (bypasses visibility)
- **Validation**: Spike test confirms JSON → object with private setters works

### Projection Object Initializers
- **Status**: ✅ Already compatible
- **Pattern**: `new ReadModel { Prop1 = value1, Prop2 = value2 }`
- **No changes needed**: Object initializers work with private setters

### Patch Operations
- **Status**: ✅ Compatible
- **Reason**: Marten's Patch API uses reflection to set properties (bypasses visibility)
- **Validation**: Spike test confirms Patch via reflection works with private setters

## Implementation Order

**Recommended sequence** (can execute in parallel within groups):

1. **Group 1** (Independent, simple): PasskeyProjection, ApiKeyProjection (UserManagement) - no collections, no dependencies
2. **Group 2** (Independent, simple): SubdomainLookup, ChaosAddressLookup (DomainManagement) - no collections, no dependencies
3. **Group 3** (Collection handling): UserLookup (UserManagement) - 3 collections, verify projection patches work
4. **Group 4** (Nested collections): DomainLookup (DomainManagement) - 2 nested collections, requires careful projection testing
5. **Group 5** (Simple, no dependencies): EmailLookup, CampaignSummary (EmailInbox) - no collections

**Parallel execution opportunities**:
- Groups 1, 2, 5 can run independently (no shared projections)
- Group 3 (UserLookup) waits for Group 1 completion
- Group 4 (DomainLookup) waits for Group 2 completion

---

## Files to Modify

### Readmodel Classes (8 files)
```
src/modules/Spamma.Modules.UserManagement/Infrastructure/ReadModels/
  ├── UserLookup.cs
  ├── PasskeyProjection.cs
  └── ApiKeyProjection.cs

src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/
  ├── DomainLookup.cs
  ├── SubdomainLookup.cs
  └── ChaosAddressLookup.cs

src/modules/Spamma.Modules.EmailInbox/Infrastructure/ReadModels/
  ├── EmailLookup.cs
  └── CampaignSummary.cs
```

### Projection Tests (Verification, no changes)
```
tests/Spamma.Modules.UserManagement.Tests/Infrastructure/Projections/
  └── *ProjectionTests.cs (existing tests verify compatibility)

tests/Spamma.Modules.DomainManagement.Tests/Infrastructure/Projections/
  └── *ProjectionTests.cs (existing tests verify compatibility)

tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/Projections/
  └── *ProjectionTests.cs (existing tests verify compatibility)
```

---

## Validation Checklist

- [ ] All 8 readmodels converted to private setters
- [ ] No public setters remain (code analysis: 0 violations)
- [ ] All collection properties use `{ get; } = new()`
- [ ] Build succeeds with zero warnings
- [ ] All existing projection tests pass unchanged
- [ ] Backward compatibility test passes (existing documents deserialize)
