using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.User;

internal class StartAuthenticationCommandHandler(
    IUserRepository userRepository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher integrationEventPublisher,
    IEnumerable<IValidator<StartAuthenticationCommand>> validators,
    ILogger<StartAuthenticationCommandHandler> logger) : CommandHandler<StartAuthenticationCommand>(validators, logger)
{
    private readonly ILogger<StartAuthenticationCommandHandler> _logger = logger;

    protected override async Task<CommandResult> HandleInternal(StartAuthenticationCommand request, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Magic link authentication started for {EmailAddress}", request.EmailAddress);

        var userMaybe = await userRepository.GetByEmailAddressAsync(request.EmailAddress, cancellationToken);
        if (userMaybe.HasNoValue)
        {
            this._logger.LogWarning("Magic link authentication failed — no user found for {EmailAddress}", request.EmailAddress);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with Email Address {request.EmailAddress} not found"));
        }

        var user = userMaybe.Value;
        this._logger.LogDebug("Found user {UserId} for {EmailAddress}", user.Id, request.EmailAddress);

        var result = user.StartAuthentication(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            this._logger.LogWarning("StartAuthentication domain call failed for {UserId}: {ErrorCode}", user.Id, result.Error);
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await userRepository.SaveAsync(user, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            this._logger.LogError("Failed to persist authentication attempt for {UserId}", user.Id);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        this._logger.LogDebug("Authentication attempt {AttemptId} persisted for {UserId}", result.Value.AuthenticationAttemptId, user.Id);

        await integrationEventPublisher.PublishAsync(
            new AuthenticationStartedIntegrationEvent(
            user.Id,
            user.SecurityStamp,
            result.Value.AuthenticationAttemptId,
            user.Name,
            user.EmailAddress,
            timeProvider.GetUtcNow().UtcDateTime), cancellationToken);

        this._logger.LogInformation(
            "AuthenticationStarted integration event published for {UserId} ({EmailAddress}), attempt {AttemptId}",
            user.Id,
            user.EmailAddress,
            result.Value.AuthenticationAttemptId);

        return CommandResult.Succeeded();
    }
}