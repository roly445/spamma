using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands.User;

public record StartAuthenticationCommand(string EmailAddress) : ICommand;