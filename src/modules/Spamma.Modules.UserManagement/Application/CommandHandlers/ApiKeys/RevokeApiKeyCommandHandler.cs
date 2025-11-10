using System.Security.Claims;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.ApiKey;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;
using Spamma.Modules.UserManagement.Client.Contracts;
using ApiKeyAggregate = Spamma.Modules.UserManagement.Domain.ApiKeys.ApiKey;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers.ApiKeys;

internal class RevokeApiKeyCommandHandler(
    IApiKeyRepository apiKeyRepository,
    IIntegrationEventPublisher eventPublisher,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    IEnumerable<IValidator<RevokeApiKeyCommand>> validators,
    ILogger<RevokeApiKeyCommandHandler> logger) : CommandHandler<RevokeApiKeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.InvalidAuthenticationAttempt));
        }

        // Get the API key
        var apiKeyResult = await apiKeyRepository.GetByIdAsync(request.ApiKeyId, cancellationToken);
        if (apiKeyResult.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyNotFound));
        }

        var apiKey = apiKeyResult.Value;

        // Verify the API key belongs to the current user
        if (apiKey.UserId != currentUserId)
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyNotFound));
        }

        // Check if already revoked
        if (apiKey.IsRevoked)
        {
            return CommandResult.Failed(new BluQubeErrorData(UserManagementErrorCodes.ApiKeyAlreadyRevoked));
        }

        // Revoke the API key
        var now = timeProvider.GetUtcNow();
        apiKey.Revoke(now);

        // Save the changes
        var saveResult = await apiKeyRepository.SaveAsync(apiKey, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        // Publish integration event
        await eventPublisher.PublishAsync(
            new ApiKeyRevokedIntegrationEvent(
                apiKey.Id,
                currentUserId,
                now.UtcDateTime),
            cancellationToken);

        return CommandResult.Succeeded();
    }
}