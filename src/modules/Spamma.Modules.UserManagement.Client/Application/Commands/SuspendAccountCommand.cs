using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/user-management/suspend-account")]
public record SuspendAccountCommand(Guid UserId, AccountSuspensionReason Reason, string? Notes) : ICommand;