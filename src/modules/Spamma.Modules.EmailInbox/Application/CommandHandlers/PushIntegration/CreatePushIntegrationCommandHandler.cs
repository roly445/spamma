using System.Security.Claims;
using BluQube.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;
using PushIntegrationAggregate = Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.PushIntegration;

/// <summary>
/// Handler for creating push integrations.
/// </summary>
internal class CreatePushIntegrationCommandHandler(
    IPushIntegrationRepository pushIntegrationRepository,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IValidator<CreatePushIntegrationCommand>> validators,
    ILogger<CreatePushIntegrationCommandHandler> logger)
    : CommandHandler<CreatePushIntegrationCommand>(validators, logger)
{
    /// <inheritdoc />
    protected override async Task<CommandResult> HandleInternal(
        CreatePushIntegrationCommand command,
        CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return CommandResult.Failed(new BluQubeErrorData("InvalidAuthentication", "Invalid authentication"));
        }

        var pushIntegrationId = Guid.NewGuid();
        var pushIntegration = PushIntegrationAggregate.Create(
            pushIntegrationId,
            currentUserId,
            command.SubdomainId,
            command.Name,
            command.EndpointUrl,
            (Domain.PushIntegrationAggregate.FilterType)command.FilterType,
            command.FilterValue,
            timeProvider.GetUtcNow());

        await pushIntegrationRepository.SaveAsync(pushIntegration, cancellationToken);

        return CommandResult.Succeeded();
    }
}