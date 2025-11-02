using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

public class SuspendSubdomainCommandHandler(
    ISubdomainRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<SuspendSubdomainCommand>> validators,
    ILogger<SuspendSubdomainCommandHandler> logger)
    : CommandHandler<SuspendSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(SuspendSubdomainCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var subdomain = subdomainMaybe.Value;
        var suspendResult = subdomain.Suspend(request.Reason, request.Notes, timeProvider.GetUtcNow().UtcDateTime);
        if (suspendResult.IsFailure)
        {
            return CommandResult.Failed(suspendResult.Error);
        }

        var saveResult = await repository.SaveAsync(subdomain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}