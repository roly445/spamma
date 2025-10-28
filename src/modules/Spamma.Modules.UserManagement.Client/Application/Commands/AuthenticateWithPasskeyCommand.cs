using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

/// <summary>
/// Command to authenticate using a passkey (no email required).
/// </summary>
[BluQubeCommand(Path = "api/user-management/authenticate-passkey")]
public record AuthenticateWithPasskeyCommand(
    byte[] CredentialId,
    uint SignCount) : ICommand;