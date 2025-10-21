using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

public record CreateDomainCommandResult(string VerificationToken) : ICommandResult;