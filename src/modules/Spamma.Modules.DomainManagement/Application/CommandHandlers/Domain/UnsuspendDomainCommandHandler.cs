using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

internal class UnsuspendDomainCommandHandler(IDomainRepository repository, TimeProvider timeProvider, IEnumerable<IValidator<UnsuspendDomainCommand>> validators, ILogger<UnsuspendDomainCommandHandler> logger)
    : CommandHandler<UnsuspendDomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UnsuspendDomainCommand request, CancellationToken cancellationToken)
    {
        var domainMaybe = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Domain with ID {request.DomainId} not found"));
        }

        var domain = domainMaybe.Value;
        var unsuspendResult = domain.Unsuspend(timeProvider.GetUtcNow().UtcDateTime);
        if (unsuspendResult.IsFailure)
        {
            return CommandResult.Failed(unsuspendResult.Error);
        }

        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}