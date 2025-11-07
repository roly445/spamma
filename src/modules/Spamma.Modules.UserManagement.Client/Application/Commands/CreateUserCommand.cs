using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/user-management/create-user")]
public record CreateUserCommand(Guid UserId, string Name, string EmailAddress, bool SendWelcome, SystemRole SystemRole) : ICommand;