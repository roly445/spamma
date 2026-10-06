using BluQube.Commands;
using BluQube.Constants;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

internal class AddViewerToSubdomainCommandHandler(
    ISubdomainRepository repository, TimeProvider timeProvider,
    IEnumerable<IValidator<AddViewerToSubdomainCommand>> validators,
    ILogger<AddViewerToSubdomainCommandHandler> logger,
    IIntegrationEventPublisher eventPublisher,
    IDocumentSession session)
    : CommandHandler<AddViewerToSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AddViewerToSubdomainCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var user = await session.LoadAsync<UserLookup>(request.UserId, cancellationToken);
        if (user is null)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with ID {request.UserId} not found"));
        }

        var subdomain = subdomainMaybe.Value;
        var addResult = subdomain.AddViewer(request.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (addResult.IsFailure)
        {
            return CommandResult.Failed(addResult.Error);
        }

        var saveResult = await repository.SaveAsync(subdomain, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        await eventPublisher.PublishAsync(
            new UserAddedAsSubdomainViewerIntegrationEvent(
                request.UserId,
                request.SubdomainId,
                user.Name,
                user.EmailAddress),
            cancellationToken);
        return CommandResult.Succeeded();
    }
}
