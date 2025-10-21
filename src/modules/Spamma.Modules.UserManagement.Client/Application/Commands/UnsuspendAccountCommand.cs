using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/user-management/unsuspend-account")]
public record UnsuspendAccountCommand(Guid UserId) : ICommand;