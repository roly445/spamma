using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/user-management/authenticate-passkey")]
public record AuthenticateWithPasskeyCommand(byte[] CredentialId, uint SignCount) : ICommand;