using BluQube.Commands;

namespace Spamma.Modules.UserManagement.Client.Application.Commands;

public record StartAuthenticationCommand(string EmailAddress) : ICommand;