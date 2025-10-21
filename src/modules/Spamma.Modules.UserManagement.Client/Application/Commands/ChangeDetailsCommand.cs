using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/user-management/change-details")]
public record ChangeDetailsCommand(Guid UserId, string EmailAddress, string Name, SystemRole SystemRole) : ICommand;