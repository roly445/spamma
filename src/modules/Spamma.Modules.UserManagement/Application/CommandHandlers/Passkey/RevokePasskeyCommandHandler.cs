using System.Security.Claims;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.Passkey;

internal class RevokePasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IValidator<RevokePasskeyCommand>> validators,
    ILogger<RevokePasskeyCommandHandler> logger) : CommandHandler<RevokePasskeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RevokePasskeyCommand request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;

        var claim = context?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var currentUserId))
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.InvalidAuthenticationAttempt));
        }

        var passkeyMaybe = await passkeyRepository.GetByIdAsync(request.PasskeyId, cancellationToken);
        if (passkeyMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.PasskeyNotFound, "Passkey not found"));
        }

        var passkey = passkeyMaybe.Value;

        if (passkey.UserId != currentUserId)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, "Passkey not found"));
        }

        var result = passkey.Revoke(currentUserId, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await passkeyRepository.SaveAsync(passkey, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}