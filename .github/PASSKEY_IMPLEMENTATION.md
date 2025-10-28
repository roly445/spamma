# Passkey Authentication Implementation Guide

This document outlines the complete passkey (WebAuthn) authentication system implemented for Spamma, allowing users to log in with passkeys without entering an email address.

## Architecture Overview

### Key Design Principles

- **Passkey-first identification**: Passkeys serve as both authenticator and user identifier (no email required for passkey login)
- **Multiple passkeys per user**: Users can register and manage multiple passkeys across different devices
- **Non-revocable by default**: Users can revoke their own passkeys; User Management admins can revoke any user's passkey
- **Replay attack prevention**: Sign count tracking prevents credential cloning attacks
- **Clean Architecture**: Follows CQRS with event sourcing pattern consistent with existing Spamma modules

## Domain Model: PasskeyAggregate

### Entities

**Passkey** (`Domain/PasskeyAggregate/Passkey.cs`)
```
Id (Guid)                    - Unique identifier for this passkey record
UserId (Guid)                - Owner of the passkey
CredentialId (byte[])        - WebAuthn credential ID (public identifier)
PublicKey (byte[])           - Public key for verification during authentication
SignCount (uint)             - Authenticator's signature counter (replay prevention)
DisplayName (string)         - User-friendly name ("My iPhone", "Windows Hello", etc.)
Algorithm (string)           - Credential algorithm ("ES256", "RS256", etc.)
IsRevoked (bool)             - Whether passkey has been revoked
RegisteredAt (DateTime)      - When passkey was registered
LastUsedAt (DateTime?)       - Last successful authentication timestamp
RevokedAt (DateTime?)        - When passkey was revoked (if applicable)
RevokedByUserId (Guid?)      - Who revoked it (null for self-revocation)
```

### Domain Events

**PasskeyRegistered**
- Raised when a new passkey is registered for a user
- Contains: UserId, CredentialId, PublicKey, SignCount, DisplayName, Algorithm, RegisteredAt

**PasskeyAuthenticated**
- Raised after successful authentication using a passkey
- Updates: SignCount (for replay prevention), LastUsedAt

**PasskeyRevoked**
- Raised when a passkey is revoked
- Contains: RevokedByUserId (tracks who revoked it), RevokedAt

### Domain Logic

#### Register a Passkey
```csharp
var result = Passkey.Register(
    userId,                    // UUID of the user
    credentialId,             // byte[] from WebAuthn
    publicKey,                // byte[] from WebAuthn
    signCount,                // uint from authenticator
    displayName,              // "My iPhone"
    algorithm,                // "ES256"
    DateTime.UtcNow
);

// Returns: Result<Passkey, BluQubeErrorData>
// Validates: UserId not empty, CredentialId/PublicKey not empty, DisplayName not null
```

#### Record Authentication
```csharp
var result = passkey.RecordAuthentication(
    newSignCount,             // uint from authenticator verification
    DateTime.UtcNow
);

// Returns: ResultWithError<BluQubeErrorData>
// Checks:
//   - Passkey is not revoked (error: PasskeyRevoked)
//   - Sign count didn't decrease (prevents cloning: PasskeyClonedOrInvalid)
```

#### Revoke a Passkey
```csharp
var result = passkey.Revoke(
    revokedByUserId,          // UUID of user doing revocation
    DateTime.UtcNow
);

// Returns: ResultWithError<BluQubeErrorData>
// Checks: Passkey is not already revoked (error: PasskeyAlreadyRevoked)
```

## Application Layer

### Commands

**RegisterPasskeyCommand**
- Request: `CredentialId`, `PublicKey`, `SignCount`, `DisplayName`, `Algorithm`
- Handler: `RegisterPasskeyCommandHandler`
- Authorization: Requires authenticated user (registers for current user)
- Response: Succeeded/Failed
- TODO: Integrate with WebAuthn verification (verify credential challenge/response on backend)

**AuthenticateWithPasskeyCommand**
- Request: `CredentialId`, `SignCount` (verified credentials from WebAuthn)
- Handler: `AuthenticateWithPasskeyCommandHandler`
- Authorization: Public (no auth required)
- Response: JWT token for authenticated API access (TODO: implement token issuing)
- Key Point: No email required; credential ID maps directly to user

**RevokePasskeyCommand**
- Request: `PasskeyId` (to revoke)
- Handler: `RevokePasskeyCommandHandler`
- Authorization: User can only revoke their own passkeys
- Response: Succeeded/Failed

**RevokeUserPasskeyCommand**
- Request: `UserId`, `PasskeyId`
- Handler: `RevokeUserPasskeyCommandHandler`
- Authorization: UserManagement admin only
- Response: Succeeded/Failed

### Queries

**GetMyPasskeysQuery**
- Returns: List of user's passkeys with metadata
- Result: `PasskeySummary[]` containing: Id, DisplayName, Algorithm, RegisteredAt, LastUsedAt, IsRevoked, RevokedAt
- Processor: `GetMyPasskeysQueryProcessor`

**GetUserPasskeysQuery**
- Request: `UserId`
- Authorization: UserManagement admin only
- Returns: User's passkeys
- Processor: `GetUserPasskeysQueryProcessor`

**GetPasskeyDetailsQuery**
- Request: `PasskeyId`
- Returns: Full passkey details including `RevokedByUserId`
- Authorization: Owner or UserManagement admin
- Processor: `GetPasskeyDetailsQueryProcessor`

### Validators

Create FluentValidation rules for:
- `RegisterPasskeyCommand`: Validate CredentialId/PublicKey not empty, DisplayName not null
- `AuthenticateWithPasskeyCommand`: Validate CredentialId not empty
- `RevokePasskeyCommand`: Validate PasskeyId not empty
- `RevokeUserPasskeyCommand`: Validate UserId/PasskeyId not empty

Files: `Application/Validators/PasskeyCommandValidators.cs`

### Authorization Policies

**OwnedPasskeyRequirement**
- User can only access/revoke passkeys they own
- Implement: `Application/AuthorizationRequirements/OwnedPasskeyRequirement.cs`
- Usage: Authorize queries/commands to verify ownership

**UserManagementAdminRequirement**
- Required for admin passkey operations
- Check user's SystemRole == SystemRole.UserManagement
- Implement: `Application/Authorizers/UserManagementAdminAuthorizer.cs`

## Repository Layer

### IPasskeyRepository
Extends `IRepository<Passkey>` (provides GetByIdAsync, SaveAsync)

Additional methods:
```csharp
Task<Maybe<Passkey>> GetByCredentialIdAsync(byte[] credentialId, CancellationToken ct);
Task<IEnumerable<Passkey>> GetByUserIdAsync(Guid userId, CancellationToken ct);
Task<IEnumerable<Passkey>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);
```

### PasskeyRepository
- Extends `GenericRepository<Passkey>`
- Uses Marten for event sourcing
- Queries read model (Marten projections) for efficient lookups by credential ID or user

## Integration Events

Create integration events in `Spamma.Modules.Common.IntegrationEvents.UserManagement`:

**PasskeyRegisteredIntegrationEvent**
- Published after `RegisterPasskeyCommand` succeeds
- Payload: UserId, PasskeyId, DisplayName, RegisteredAt
- Subscribers: Security audit logs, notification services

**PasskeyRevokedIntegrationEvent**
- Published after `RevokePasskeyCommand` or `RevokeUserPasskeyCommandHandler` succeeds
- Payload: UserId, PasskeyId, RevokedByUserId, RevokedAt
- Subscribers: Security audit logs, active session invalidation

## Error Codes

New error codes added to `UserManagementErrorCodes`:
```csharp
public const string InvalidPasskeyRegistration = "user_management.invalid_passkey_registration";
public const string PasskeyRevoked = "user_management.passkey_revoked";
public const string PasskeyClonedOrInvalid = "user_management.passkey_cloned_or_invalid";
public const string PasskeyAlreadyRevoked = "user_management.passkey_already_revoked";
public const string PasskeyNotFound = "user_management.passkey_not_found";
```

## Frontend Implementation (TODO)

### WebAuthn API Interop
Create TypeScript utilities for WebAuthn operations:
- `AssetScripts/webauthn-utils.ts`
  - `registerPasskey()`: Call navigator.credentials.create(), get credential data
  - `authenticateWithPasskey()`: Call navigator.credentials.get()
  - `parseCredentialResponse()`: Extract CredentialId, PublicKey, SignCount

### UI Components
- `Components/Passkey/RegisterPasskeyModal.razor`
  - Trigger WebAuthn registration
  - Call `RegisterPasskeyCommand` with WebAuthn response
  - Display success/error messages

- `Components/Passkey/LoginWithPasskey.razor`
  - Trigger WebAuthn authentication
  - Call `AuthenticateWithPasskeyCommand`
  - Receive JWT token on success

- `Components/Settings/ManagePasskeys.razor`
  - List user's passkeys (query: `GetMyPasskeysQuery`)
  - Button to add new passkey → `RegisterPasskeyModal`
  - Each passkey: DisplayName, RegisteredAt, LastUsedAt, Revoke button

### Setup Wizard Integration
Update `Components/Pages/Setup/Admin.razor` to offer passkey registration during initial setup.

## Testing Strategy

### Domain Tests (Unit)
File: `tests/Spamma.Modules.UserManagement.Tests/Domain/PasskeyAggregateTests.cs`

Test the Passkey aggregate methods:
- `Passkey.Register()` - happy path, validation failures
- `passkey.RecordAuthentication()` - success, revoked passkey, cloning detection
- `passkey.Revoke()` - success, already revoked error
- Event emission verification

### Command Handler Tests (Integration)
File: `tests/Spamma.Modules.UserManagement.Tests/Application/CommandHandlers/PasskeyCommandHandlerTests.cs`

Using Moq with Strict behavior:
- Mock `IPasskeyRepository` (Strict)
- Mock `IIntegrationEventPublisher` (Strict)
- Mock `TimeProvider` for deterministic timestamps
- Test: Command execution, repository calls, event publishing

### Query Handler Tests
File: `tests/Spamma.Modules.UserManagement.Tests/Application/QueryProcessors/PasskeyQueryProcessorTests.cs`

- Mock repository to return test passkeys
- Verify query results contain expected data
- Test authorization checks (access denial, admin access)

### Test Builders
File: `tests/Spamma.Modules.UserManagement.Tests/Builders/PasskeyBuilder.cs`

Fluent builder for test data:
```csharp
new PasskeyBuilder()
    .WithUserId(userId)
    .WithDisplayName("My iPhone")
    .WithAlgorithm("ES256")
    .WithLastUsedAt(DateTime.UtcNow.AddDays(-1))
    .Build();
```

## API Endpoints (TODO)

Register in `AddUserManagementApi()`:

```
POST   /api/passkeys/register           - RegisterPasskeyCommand
POST   /api/passkeys/authenticate       - AuthenticateWithPasskeyCommand
GET    /api/passkeys                    - GetMyPasskeysQuery
GET    /api/passkeys/{passkeyId}        - GetPasskeyDetailsQuery
DELETE /api/passkeys/{passkeyId}        - RevokePasskeyCommand
DELETE /api/users/{userId}/passkeys/{passkeyId} - RevokeUserPasskeyCommand (admin only)
```

## Implementation Checklist

- [x] Domain aggregate: PasskeyAggregate with Register, RecordAuthentication, Revoke methods
- [x] Domain events: PasskeyRegistered, PasskeyAuthenticated, PasskeyRevoked
- [x] Error codes: Added to UserManagementErrorCodes
- [x] Commands: RegisterPasskey, AuthenticateWithPasskey, RevokePasskey, RevokeUserPasskey
- [x] Queries: GetMyPasskeys, GetUserPasskeys, GetPasskeyDetails
- [x] Repository: IPasskeyRepository, PasskeyRepository implementation
- [x] Event handlers (empty stubs)
- [x] Module registration: Added IPasskeyRepository to DI

**TODO:**
- [ ] Authorization policies (OwnedPasskeyRequirement, UserManagementAdminAuthorizer)
- [ ] Command validators (FluentValidation)
- [ ] Integration events (PasskeyRegisteredIntegrationEvent, PasskeyRevokedIntegrationEvent)
- [ ] Authentication context retrieval (get current user ID from HttpContext in handlers)
- [ ] JWT token generation in AuthenticateWithPasskeyCommand
- [ ] Marten projection for Passkey read model
- [ ] API endpoints registration
- [ ] WebAuthn verification on backend (validate signature, challenge, etc.)
- [ ] Frontend WebAuthn utilities (TypeScript)
- [ ] Frontend components (Login, Register, Management)
- [ ] Domain tests
- [ ] Command/query handler tests
- [ ] Integration tests
- [ ] Setup wizard integration

## Security Considerations

1. **Credential Verification**: Each registration and authentication requires backend verification of WebAuthn responses (signature validation, challenge verification)
2. **Sign Count Validation**: Detects potential credential cloning
3. **Revocation Audit Trail**: Tracks who revoked each passkey
4. **No Password Fallback**: Passkey login requires actual device authentication
5. **HTTPS Required**: WebAuthn requires secure context
6. **CSRF Protection**: Standard for all API endpoints
7. **Rate Limiting**: Recommended on authentication endpoints

## Future Enhancements

- Conditional mediation UI (platform-specific passkey selection)
- Backup codes for account recovery
- Passkey backup/sync across devices
- Biometric unlock without passkey re-verification
- Enterprise enrollment policies
- Passwordless flow with email verification fallback
