using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.User;

[BluQubeCommand(Path = "api/user-management/change-details")]
public record ChangeDetailsCommand(Guid UserId, string EmailAddress, string Name, SystemRole SystemRole) : ICommand;