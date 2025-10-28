using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

/// <summary>
/// Command to revoke a passkey on behalf of another user (admin only).
/// </summary>
[BluQubeCommand(Path = "api/user-management/revoke-user-passkey")]
public record RevokeUserPasskeyCommand(Guid UserId, Guid PasskeyId) : ICommand;