using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

internal class RemoveViewerFromSubdomainCommandHandler(
    ISubdomainRepository repository, TimeProvider timeProvider,
    IEnumerable<IValidator<RemoveViewerFromSubdomainCommand>> validators,
    ILogger<RemoveViewerFromSubdomainCommandHandler> logger,
    IIntegrationEventPublisher eventPublisher)
    : CommandHandler<RemoveViewerFromSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RemoveViewerFromSubdomainCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var subdomain = subdomainMaybe.Value;
        var removeResult = subdomain.RemoveViewer(request.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (removeResult.IsFailure)
        {
            return CommandResult.Failed(removeResult.Error);
        }

        var saveResult = await repository.SaveAsync(subdomain, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        await eventPublisher.PublishAsync(new UserRemovedFromBeingSubdomainViewerIntegrationEvent(request.UserId, request.SubdomainId), cancellationToken);
        return CommandResult.Succeeded();
    }
}