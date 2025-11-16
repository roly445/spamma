using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

[BluQubeCommand(Path = "api/user-management/api-keys")]
public record CreateApiKeyCommand(string Name) : ICommand<CreateApiKeyCommandResult>
{
    public DateTimeOffset WhenExpires { get; set; }
}