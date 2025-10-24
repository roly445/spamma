﻿using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

public class AddViewerToSubdomainCommandHandler(
    ISubdomainRepository repository, TimeProvider timeProvider,
    IEnumerable<IValidator<AddViewerToSubdomainCommand>> validators,
    ILogger<AddViewerToSubdomainCommandHandler> logger,
    IIntegrationEventPublisher eventPublisher)
    : CommandHandler<AddViewerToSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AddViewerToSubdomainCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
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

        await eventPublisher.PublishAsync(new UserAddedAsSubdomainViewerIntegrationEvent(request.UserId, request.SubdomainId), cancellationToken);
        return CommandResult.Succeeded();
    }
}