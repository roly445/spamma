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