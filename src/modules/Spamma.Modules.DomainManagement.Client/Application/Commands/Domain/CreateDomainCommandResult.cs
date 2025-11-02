using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

public record CreateDomainCommandResult(string VerificationToken) : ICommandResult;