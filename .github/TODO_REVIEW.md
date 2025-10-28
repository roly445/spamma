# TODO Statement Review - October 28, 2025

## Summary
Found **4 active TODO comments** in the codebase, all related to passkey management authorization and user context retrieval. Additionally found **1 typo** that should be fixed.

---

## ACTIVE TODOs (4 items)

### 1. ⚠️ RevokeUserPasskeyCommandHandler - User Context Not Implemented
**File**: `src/modules/Spamma.Modules.UserManagement/Application/CommandHandlers/Passkey/RevokeUserPasskeyCommandHandler.cs` (Lines 22-25)

**Status**: ❌ STILL VALID - INCOMPLETE IMPLEMENTATION

**TODOs**:
```csharp
// TODO: Get current authenticated user ID from context/claims
var currentUserId = Guid.Empty; // Replace with actual authentication context

// TODO: Verify current user has UserManagement role
// This should be done via an authorization requirement/policy
```

**Context**: This command is for revoking another user's passkey (admin/user management role). Currently hardcoded to `Guid.Empty`.

**Required Fixes**:
- Inject `IHttpContextAccessor` to get current user ID from claims (like in `RevokePasskeyCommandHandler`)
- Add authorization policy check for UserManagement role
- Use current user ID to verify they have authority to revoke other user's passkeys

**Severity**: 🔴 HIGH - Security issue, allows unauthorized passkey revocation

---

### 2. ⚠️ GetUserPasskeysQueryProcessor - Authorization Check Missing
**File**: `src/modules/Spamma.Modules.UserManagement/Application/QueryProcessors/Passkey/GetUserPasskeysQueryProcessor.cs` (Line 20)

**Status**: ❌ STILL VALID - AUTHORIZATION NOT ENFORCED

**TODO**:
```csharp
// TODO: Verify current user has UserManagement role (authorization policy)
```

**Context**: This query retrieves all passkeys for a specific user (admin feature). Currently has no authorization check.

**Required Fixes**:
- Implement authorization requirement to verify user has UserManagement role
- May need to use `IHttpContextAccessor` to get current user context
- Consider pattern used in DomainManagement with `MustBeModeratorToDomainRequirement`

**Severity**: 🔴 HIGH - Information disclosure risk, any authenticated user can query other user's passkeys

---

### 3. ⚠️ GetPasskeyDetailsQueryProcessor - Authorization Check Missing
**File**: `src/modules/Spamma.Modules.UserManagement/Application/QueryProcessors/Passkey/GetPasskeyDetailsQueryProcessor.cs` (Line 27)

**Status**: ❌ STILL VALID - AUTHORIZATION NOT ENFORCED

**TODO**:
```csharp
// TODO: Verify the user has access to view this passkey
// (either owns it or is a user management admin)
```

**Context**: This query retrieves detailed passkey information. Needs to verify caller either owns the passkey or is an admin.

**Required Fixes**:
- Get current user ID from HTTP context/claims
- Verify current user either:
  - Owns the passkey (userId matches), OR
  - Has UserManagement role
- Return NotFound/Unauthorized if unauthorized

**Severity**: 🔴 HIGH - Information disclosure risk, exposes other user's passkey details

---

### 4. ⚠️ RevokeUserPasskeyCommandHandler - Incomplete Implementation
**File**: `src/modules/Spamma.Modules.UserManagement/Application/CommandHandlers/Passkey/RevokeUserPasskeyCommandHandler.cs` (Lines 22-25)

**Related**: Same file, already listed as #1 above

**Context**: This is the admin command to revoke any user's passkey (vs `RevokePasskeyCommandHandler` which revokes authenticated user's own passkeys).

**Implementation Gap**: Currently non-functional due to missing context and authorization.

---

## BUGS FOUND (1 item)

### 🐛 Typo in AccountSuspensionAudit
**File**: `src/modules/Spamma.Modules.UserManagement/Domain/UserAggregate/AccountSuspensionAudit.cs` (Line 20)

**Issue**: Typo in error message - "Notw" instead of "Note"

**Current Code**:
```csharp
public string Notes => this.Type == AccountSuspensionAuditType.Unsuspend 
    ? throw new InvalidOperationException("Notw is not applicable for unsuspension.") 
    : this._notes!;
```

**Should Be**:
```csharp
public string Notes => this.Type == AccountSuspensionAuditType.Unsuspend 
    ? throw new InvalidOperationException("Notes are not applicable for unsuspension.") 
    : this._notes!;
```

**Severity**: 🟡 LOW - Only visible if code path is executed, cosmetic issue

---

## RECOMMENDATIONS

### Priority 1 (Security Critical)
1. ✅ Implement `RevokeUserPasskeyCommandHandler` user context and authorization
   - Pattern to follow: `RevokePasskeyCommandHandler` implementation
   - Add `IHttpContextAccessor` injection
   - Extract user ID from claims
   - Add authorization policy check

2. ✅ Implement authorization in `GetUserPasskeysQueryProcessor`
   - Add authorization requirement handler
   - Similar to DomainManagement's `MustBeModeratorToDomainRequirement`
   - Validate UserManagement role

3. ✅ Implement authorization in `GetPasskeyDetailsQueryProcessor`
   - Get current user context
   - Verify ownership OR admin role
   - Return appropriate error responses

### Priority 2 (Code Quality)
4. ✅ Fix typo in `AccountSuspensionAudit`
   - "Notw" → "Notes are"

---

## NOTES

- The pattern for proper implementation already exists in `RevokePasskeyCommandHandler` which correctly:
  - Injects `IHttpContextAccessor`
  - Extracts user ID from claims
  - Validates ownership before revoking
  - Returns appropriate error codes

- The passkey feature is functionally complete for authenticated users managing their own passkeys
- These TODOs represent admin/management features that are incomplete

- Once these are fixed, consider adding integration tests for:
  - Admin revoking other user's passkeys
  - Authorization failures (non-admin attempting to revoke)
  - Query authorization checks
