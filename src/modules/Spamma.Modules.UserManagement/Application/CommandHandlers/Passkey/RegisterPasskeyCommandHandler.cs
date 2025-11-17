using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Client.Contracts;
using PasskeyAggregate = Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.Passkey;

internal class RegisterPasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IValidator<RegisterPasskeyCommand>> validators,
    ILogger<RegisterPasskeyCommandHandler> logger) : CommandHandler<RegisterPasskeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RegisterPasskeyCommand request, CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userAuthInfo = httpContextAccessor.HttpContext.ToUserAuthInfo();

        if (!userAuthInfo.IsAuthenticated)
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.InvalidAuthenticationAttempt));
        }

        var result = PasskeyAggregate.Register(
            userAuthInfo.UserId,
            request.CredentialId,
            request.PublicKey,
            request.SignCount,
            request.DisplayName,
            request.Algorithm,
            timeProvider.GetUtcNow().UtcDateTime);

        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var passkey = result.Value;
        var saveResult = await passkeyRepository.SaveAsync(passkey, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}