using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

public class AddModeratorToDomainCommandHandler(
    IDomainRepository repository, TimeProvider timeProvider,
    IEnumerable<IValidator<AddModeratorToDomainCommand>> validators,
    ILogger<AddModeratorToDomainCommandHandler> logger,
    IIntegrationEventPublisher eventPublisher)
    : CommandHandler<AddModeratorToDomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AddModeratorToDomainCommand request, CancellationToken cancellationToken)
    {
        var domainMaybe = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Domain with ID {request.DomainId} not found"));
        }

        var domain = domainMaybe.Value;
        var addResult = domain.AddModerationUser(request.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (addResult.IsFailure)
        {
            return CommandResult.Failed(addResult.Error);
        }

        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        await eventPublisher.PublishAsync(new UserAddedAsDomainModeratorIntegrationEvent(request.UserId, request.DomainId), cancellationToken);
        return CommandResult.Succeeded();
    }
}