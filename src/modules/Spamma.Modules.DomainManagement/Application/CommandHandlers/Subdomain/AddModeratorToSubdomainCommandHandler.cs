using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

internal class AddModeratorToSubdomainCommandHandler(
    ISubdomainRepository repository, TimeProvider timeProvider,
    IEnumerable<IValidator<AddModeratorToSubdomainCommand>> validators,
    ILogger<AddModeratorToSubdomainCommandHandler> logger,
    IIntegrationEventPublisher eventPublisher,
    IQuerier querier)
    : CommandHandler<AddModeratorToSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AddModeratorToSubdomainCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var userResult = await querier.Send(new GetUserByIdQuery(request.UserId), cancellationToken);
        if (userResult.Status != QueryResultStatus.Succeeded)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with ID {request.UserId} not found"));
        }

        var subdomain = subdomainMaybe.Value;
        var addResult = subdomain.AddModerationUser(request.UserId, timeProvider.GetUtcNow().UtcDateTime);
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
            new UserAddedAsSubdomainModeratorIntegrationEvent(
                request.UserId,
                request.SubdomainId,
                userResult.Data.Name,
                userResult.Data.EmailAddress),
            cancellationToken);
        return CommandResult.Succeeded();
    }
}