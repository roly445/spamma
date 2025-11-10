using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

[BluQubeCommand(Path = "api/user-management/api-keys/revoke")]
public record RevokeApiKeyCommand(Guid ApiKeyId) : ICommand;