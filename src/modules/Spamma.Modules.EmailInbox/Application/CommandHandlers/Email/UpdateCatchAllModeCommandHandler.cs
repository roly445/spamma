using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;

internal class UpdateCatchAllModeCommandHandler(
    IEnumerable<IValidator<UpdateCatchAllModeCommand>> validators,
    ILogger<UpdateCatchAllModeCommandHandler> logger,
    IEmailInboxSettingsService settingsService,
    IIntegrationEventPublisher eventPublisher)
    : CommandHandler<UpdateCatchAllModeCommand>(validators, logger)
{
    private readonly ILogger<UpdateCatchAllModeCommandHandler> _logger = logger;

    protected override async Task<CommandResult> HandleInternal(UpdateCatchAllModeCommand request, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Updating catch-all mode: {Enabled}", request.CatchAllModeEnabled);
        await settingsService.SetCatchAllModeEnabledAsync(request.CatchAllModeEnabled, cancellationToken);
        await eventPublisher.PublishAsync(
            new SystemSettingsUpdatedIntegrationEvent(
                new GetSystemSettingsQueryResult(request.CatchAllModeEnabled)),
            cancellationToken);
        this._logger.LogInformation("Catch-all mode updated: {Enabled}", request.CatchAllModeEnabled);
        return CommandResult.Succeeded();
    }
}
