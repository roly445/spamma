using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

public class UnsuspendSubdomainCommandHandler(
    ISubdomainRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<UnsuspendSubdomainCommand>> validators,
    ILogger<UnsuspendSubdomainCommandHandler> logger)
    : CommandHandler<UnsuspendSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UnsuspendSubdomainCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var subdomain = subdomainMaybe.Value;
        var unsuspendResult = subdomain.Unsuspend(timeProvider.GetUtcNow().UtcDateTime);
        if (unsuspendResult.IsFailure)
        {
            return CommandResult.Failed(unsuspendResult.Error);
        }

        var saveResult = await repository.SaveAsync(subdomain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}