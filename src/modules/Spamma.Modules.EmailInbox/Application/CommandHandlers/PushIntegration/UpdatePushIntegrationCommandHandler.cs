using System.Security.Claims;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.PushIntegration;

/// <summary>
/// Handler for updating push integrations.
/// </summary>
internal class UpdatePushIntegrationCommandHandler(
    IPushIntegrationRepository pushIntegrationRepository,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IValidator<UpdatePushIntegrationCommand>> validators,
    ILogger<UpdatePushIntegrationCommandHandler> logger)
    : CommandHandler<UpdatePushIntegrationCommand>(validators, logger)
{
    /// <inheritdoc />
    protected override async Task<CommandResult> HandleInternal(
        UpdatePushIntegrationCommand command,
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

        pushIntegration.Update(
            command.Name,
            command.EndpointUrl,
            (Domain.PushIntegrationAggregate.FilterType)command.FilterType,
            command.FilterValue,
            timeProvider.GetUtcNow());

        await pushIntegrationRepository.SaveAsync(pushIntegration, cancellationToken);

        return CommandResult.Succeeded();
    }
}