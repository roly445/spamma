using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.User;

internal class StartAuthenticationCommandHandler(
    IUserRepository userRepository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher integrationEventPublisher,
    IEnumerable<IValidator<StartAuthenticationCommand>> validators,
    ILogger<StartAuthenticationCommandHandler> logger) : CommandHandler<StartAuthenticationCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(StartAuthenticationCommand request, CancellationToken cancellationToken)
    {
        var userMaybe = await userRepository.GetByEmailAddressAsync(request.EmailAddress, cancellationToken);
        if (userMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with Email Address {request.EmailAddress} not found"));
        }

        var user = userMaybe.Value;

        var result = user.StartAuthentication(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await userRepository.SaveAsync(user, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        await integrationEventPublisher.PublishAsync(
            new AuthenticationStartedIntegrationEvent(
            user.Id,
            user.SecurityStamp,
            result.Value.AuthenticationAttemptId,
            user.Name,
            user.EmailAddress,
            timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        return CommandResult.Succeeded();
    }
}