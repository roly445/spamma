using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.User;

public record CompleteAuthenticationCommand(Guid UserId, Guid SecurityStamp, Guid AuthenticationAttemptId) : ICommand;