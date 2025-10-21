using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

public class SuspendDomainCommandHandler(IDomainRepository repository, TimeProvider timeProvider, IEnumerable<IValidator<SuspendDomainCommand>> validators, ILogger<SuspendDomainCommandHandler> logger)
    : CommandHandler<SuspendDomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(SuspendDomainCommand request, CancellationToken cancellationToken)
    {
        var domainMaybe = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Domain with ID {request.DomainId} not found"));
        }

        var domain = domainMaybe.Value;
        var suspendResult = domain.Suspend(request.Reason, request.Notes, timeProvider.GetUtcNow().UtcDateTime);
        if (suspendResult.IsFailure)
        {
            return CommandResult.Failed(suspendResult.Error);
        }

        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}