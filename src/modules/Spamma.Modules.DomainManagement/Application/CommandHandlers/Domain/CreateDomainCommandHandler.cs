using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using DomainAggregate = Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

public class CreateDomainCommandHandler(IDomainRepository repository, IEnumerable<IValidator<CreateDomainCommand>> validators, ILogger<CreateDomainCommandHandler> logger, TimeProvider timeProvider)
    : CommandHandler<CreateDomainCommand, CreateDomainCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<CreateDomainCommandResult>> HandleInternal(CreateDomainCommand request, CancellationToken cancellationToken)
    {
        var domainResult = DomainAggregate.Create(request.DomainId, request.Name, request.PrimaryContactEmail,
            request.Description, timeProvider.GetUtcNow().UtcDateTime);

        if (domainResult.IsFailure)
        {
            return CommandResult<CreateDomainCommandResult>.Failed(domainResult.Error);
        }

        var domain = domainResult.Value;
        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ?
            CommandResult<CreateDomainCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) :
            CommandResult<CreateDomainCommandResult>.Succeeded(new CreateDomainCommandResult(domain.VerificationToken));
    }
}