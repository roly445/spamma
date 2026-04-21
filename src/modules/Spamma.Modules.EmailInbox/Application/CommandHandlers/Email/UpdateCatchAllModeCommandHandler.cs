using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;

internal class UpdateCatchAllModeCommandHandler(
    IEnumerable<IValidator<UpdateCatchAllModeCommand>> validators,
    ILogger<UpdateCatchAllModeCommandHandler> logger,
    IEmailInboxSettingsService settingsService)
    : CommandHandler<UpdateCatchAllModeCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UpdateCatchAllModeCommand request, CancellationToken cancellationToken)
    {
        await settingsService.SetCatchAllModeEnabledAsync(request.CatchAllModeEnabled, cancellationToken);
        return CommandResult.Succeeded();
    }
}
