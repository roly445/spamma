using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

[BluQubeCommand(Path = "api/user-management/revoke-passkey")]
public record RevokePasskeyCommand(Guid PasskeyId) : ICommand;