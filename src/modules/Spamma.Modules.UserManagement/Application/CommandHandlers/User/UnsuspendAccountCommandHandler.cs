using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.User;

internal class UnsuspendAccountCommandHandler(
    IUserRepository repository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher eventPublisher,
    IEnumerable<IValidator<UnsuspendAccountCommand>> validators,
    ILogger<UnsuspendAccountCommandHandler> logger) : CommandHandler<UnsuspendAccountCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UnsuspendAccountCommand request, CancellationToken cancellationToken)
    {
        var userMaybe = await repository.GetByIdAsync(request.UserId, cancellationToken);

        if (userMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with ID {request.UserId} not found"));
        }

        var user = userMaybe.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var unsuspendResult = user.Unsuspend(now);
        if (unsuspendResult.IsFailure)
        {
            return CommandResult.Failed(unsuspendResult.Error);
        }

        var saveResult = await repository.SaveAsync(user, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        await eventPublisher.PublishAsync(new UserUnsuspendedIntegrationEvent(user.Id, now), cancellationToken);

        return CommandResult.Succeeded();
    }
}