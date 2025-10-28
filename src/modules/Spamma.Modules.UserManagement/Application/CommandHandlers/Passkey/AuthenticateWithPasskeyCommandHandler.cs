using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.Passkey;

internal class AuthenticateWithPasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<AuthenticateWithPasskeyCommand>> validators,
    ILogger<AuthenticateWithPasskeyCommandHandler> logger) : CommandHandler<AuthenticateWithPasskeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AuthenticateWithPasskeyCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Searching for passkey with credential ID bytes: {CredentialIdHex} (length: {Length})",
            string.Join(" ", request.CredentialId.Select(b => b.ToString("X2"))),
            request.CredentialId.Length);

        var passkeyMaybe = await passkeyRepository.GetByCredentialIdAsync(request.CredentialId, cancellationToken);
        if (passkeyMaybe.HasNoValue)
        {
            logger.LogWarning("Passkey not found for credential ID");
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.PasskeyNotFound, "Passkey not found"));
        }

        var passkey = passkeyMaybe.Value;
        logger.LogInformation(
            "Found passkey {PasskeyId} for user {UserId}. Stored sign count: {StoredSignCount}, new sign count: {NewSignCount}",
            passkey.Id,
            passkey.UserId,
            passkey.SignCount,
            request.SignCount);

        var userMaybe = await userRepository.GetByIdAsync(passkey.UserId, cancellationToken);
        if (userMaybe.HasNoValue)
        {
            logger.LogWarning("User {UserId} not found for passkey", passkey.UserId);
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.AccountSuspended, "Account not found"));
        }

        if (userMaybe.Value.IsSuspended)
        {
            logger.LogWarning("Authentication attempt with passkey for suspended user {UserId}", passkey.UserId);
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.AccountSuspended, "Account is suspended"));
        }

        var result = passkey.RecordAuthentication(request.SignCount, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Sign count validation failed for passkey {PasskeyId}: {ErrorMessage}",
                passkey.Id,
                result.Error.Message);
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await passkeyRepository.SaveAsync(passkey, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            logger.LogWarning("Failed to save passkey {PasskeyId} after authentication", passkey.Id);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        logger.LogInformation("Successfully authenticated user {UserId} with passkey {PasskeyId}", passkey.UserId, passkey.Id);
        return CommandResult.Succeeded();
    }
}