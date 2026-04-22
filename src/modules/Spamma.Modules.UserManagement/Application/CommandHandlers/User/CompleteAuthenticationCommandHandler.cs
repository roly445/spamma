using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.User;

internal class CompleteAuthenticationCommandHandler(
    IUserRepository repository,
    TimeProvider timeProvider,
    IOptions<Settings> settings,
    IEnumerable<IValidator<CompleteAuthenticationCommand>> validators,
    ILogger<CompleteAuthenticationCommandHandler> logger) : CommandHandler<CompleteAuthenticationCommand>(validators, logger)
{
    private readonly ILogger<CompleteAuthenticationCommandHandler> _logger = logger;
    private readonly Settings _settings = settings.Value;

    protected override async Task<CommandResult> HandleInternal(CompleteAuthenticationCommand request, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Completing authentication for user {UserId}, attempt {AttemptId}",
            request.UserId,
            request.AuthenticationAttemptId);

        var userMaybe = await repository.GetByIdAsync(request.UserId, cancellationToken);

        if (userMaybe.HasNoValue)
        {
            this._logger.LogWarning("CompleteAuthentication failed — no user found for {UserId}", request.UserId);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"User with ID {request.UserId} not found"));
        }

        var user = userMaybe.Value;
        var authResult = user.ProcessAuthentication(
            request.AuthenticationAttemptId,
            request.SecurityStamp,
            timeProvider.GetUtcNow().UtcDateTime,
            this._settings.AuthenticationTimeInMinutes);

        if (authResult.IsFailure)
        {
            this._logger.LogWarning(
                "ProcessAuthentication failed for {UserId}, attempt {AttemptId}: {ErrorCode}",
                request.UserId,
                request.AuthenticationAttemptId,
                authResult.Error);
            return CommandResult.Failed(authResult.Error);
        }

        var saveResult = await repository.SaveAsync(user, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            this._logger.LogError("Failed to persist completed authentication for {UserId}", request.UserId);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        this._logger.LogInformation(
            "Authentication completed successfully for {UserId}, attempt {AttemptId}",
            request.UserId,
            request.AuthenticationAttemptId);

        return CommandResult.Succeeded();
    }
}