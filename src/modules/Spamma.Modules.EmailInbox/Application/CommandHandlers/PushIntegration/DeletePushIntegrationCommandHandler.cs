using System.Security.Claims;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.PushIntegration;

/// <summary>
/// Handler for deleting push integrations.
/// </summary>
internal class DeletePushIntegrationCommandHandler(
    IPushIntegrationRepository pushIntegrationRepository,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IValidator<DeletePushIntegrationCommand>> validators,
    ILogger<DeletePushIntegrationCommandHandler> logger)
    : CommandHandler<DeletePushIntegrationCommand>(validators, logger)
{
    /// <inheritdoc />
    protected override async Task<CommandResult> HandleInternal(
        DeletePushIntegrationCommand command,
        CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return CommandResult.Failed(new BluQubeErrorData("InvalidAuthentication", "Invalid authentication"));
        }

        var pushIntegrationMaybe = await pushIntegrationRepository.GetByIdAsync(command.IntegrationId, cancellationToken);

        if (pushIntegrationMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData("NotFound", "Push integration not found"));
        }

        var pushIntegration = pushIntegrationMaybe.Value;

        // Check ownership
        if (pushIntegration.UserId != currentUserId)
        {
            return CommandResult.Failed(new BluQubeErrorData("Forbidden", "Access denied"));
        }

        pushIntegration.Delete(timeProvider.GetUtcNow());

        await pushIntegrationRepository.SaveAsync(pushIntegration, cancellationToken);

        return CommandResult.Succeeded();
    }
}