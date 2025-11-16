using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

[BluQubeCommand(Path = "api/user-management/revoke-user-passkey")]
public record RevokeUserPasskeyCommand(Guid UserId, Guid PasskeyId) : ICommand;