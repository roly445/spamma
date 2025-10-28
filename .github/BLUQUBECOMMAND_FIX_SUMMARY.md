# BluQubeCommand Attributes - Passkey Commands Fixed

## Summary

Successfully added `BluQubeCommand` code generation attributes to all passkey commands in the client project and split them into individual files per type (StyleCop SA1649 compliance).

## What Was Done

### 1. Split Commands Into Individual Files

**Before**: Single `PasskeyCommands.cs` with 4 command records (❌ SA1649 violation)
```csharp
public record RegisterPasskeyCommand(...) : ICommand;
public record AuthenticateWithPasskeyCommand(...) : ICommand;
public record RevokePasskeyCommand(...) : ICommand;
public record RevokeUserPasskeyCommand(...) : ICommand;
```

**After**: 4 separate files, one per command (✅ SA1649 compliant)

### 2. Added BluQubeCommand Attributes

All commands now have code generation attributes with API paths:

| Command | Attribute | Purpose |
|---------|-----------|---------|
| `RegisterPasskeyCommand.cs` | `[BluQubeCommand(Path = "api/user-management/register-passkey")]` | Register new passkey |
| `AuthenticateWithPasskeyCommand.cs` | `[BluQubeCommand(Path = "api/user-management/authenticate-passkey")]` | Authenticate with passkey |
| `RevokePasskeyCommand.cs` | `[BluQubeCommand(Path = "api/user-management/revoke-passkey")]` | User revokes own passkey |
| `RevokeUserPasskeyCommand.cs` | `[BluQubeCommand(Path = "api/user-management/revoke-user-passkey")]` | Admin revokes any passkey |

## Code Example

```csharp
using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

/// <summary>
/// Command to register a new passkey for the authenticated user.
/// </summary>
[BluQubeCommand(Path = "api/user-management/register-passkey")]
public record RegisterPasskeyCommand(
    byte[] CredentialId,
    byte[] PublicKey,
    uint SignCount,
    string DisplayName,
    string Algorithm) : ICommand;
```

## Path Naming Convention

Following established pattern:
- **User Management**: `api/user-management/*`
- **Domain Management**: `api/domain-management/*`
- **Subdomain Management**: `api/subdomain-management/*`
- **Email Inbox**: `email-inbox/*`

## StyleCop Compliance

✅ **SA1649**: One type per file  
✅ **SA1600**: All public members documented  
✅ **SA1602**: Documentation ends with period  
✅ **File naming**: Matches type names  
✅ **BluQube attribute**: Present on all query/command records  

## Documentation Updated

Added comprehensive section to `.github/copilot-instructions.md`:
- Command File Structure pattern (one type per file)
- BluQubeCommand attribute usage
- Path naming conventions by module
- Code example with proper attribute

## Build Status

✅ **`Spamma.Modules.UserManagement.Client` builds successfully with 0 errors**

## Files Created

```
RegisterPasskeyCommand.cs          ✅ New
AuthenticateWithPasskeyCommand.cs  ✅ New
RevokePasskeyCommand.cs            ✅ New
RevokeUserPasskeyCommand.cs        ✅ New
PasskeyCommands.cs                 ⚠️  Deprecated (marker only)
```

## Pattern Summary

**Both queries and commands now follow:**
1. ✅ One type per file (SA1649)
2. ✅ BluQube attributes for code generation
3. ✅ Proper XML documentation
4. ✅ Clear naming conventions
5. ✅ StyleCop/SonarQube compliant

The passkey system is now fully compliant with project standards! 🎉
