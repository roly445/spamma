using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

/// <summary>
/// Command to revoke a passkey owned by the caller.
/// </summary>
[BluQubeCommand(Path = "api/user-management/revoke-passkey")]
public record RevokePasskeyCommand(Guid PasskeyId) : ICommand;