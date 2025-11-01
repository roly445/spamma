using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers;

/// <summary>
/// Command handler for toggling email favorite status to prevent automatic deletion.
/// </summary>
public class ToggleEmailFavoriteCommandHandler(
    IEmailRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<ToggleEmailFavoriteCommand>> validators,
    ILogger<ToggleEmailFavoriteCommandHandler> logger)
    : CommandHandler<ToggleEmailFavoriteCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(ToggleEmailFavoriteCommand request, CancellationToken cancellationToken)
    {
        var emailMaybe = await repository.GetByIdAsync(request.EmailId, cancellationToken);

        if (emailMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Email with ID {request.EmailId} not found"));
        }

        var email = emailMaybe.Value;
        var now = timeProvider.GetUtcNow().DateTime;

        // Toggle the favorite status
        var result = email.IsFavorite
            ? email.UnmarkAsFavorite(now)
            : email.MarkAsFavorite(now);

        if (!result.IsSuccess)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await repository.SaveAsync(email, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}