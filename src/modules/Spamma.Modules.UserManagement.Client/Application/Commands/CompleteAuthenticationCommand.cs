using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

public record CompleteAuthenticationCommand(Guid UserId, Guid SecurityStamp, Guid AuthenticationAttemptId) : ICommand;