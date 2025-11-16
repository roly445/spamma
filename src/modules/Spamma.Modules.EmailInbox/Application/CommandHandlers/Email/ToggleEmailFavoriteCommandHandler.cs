using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;

internal class ToggleEmailFavoriteCommandHandler(
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

        if (email.IsPartOfCampaign)
        {
            logger.LogWarning("Email {EmailId} is part of campaign {CampaignId}, favorite toggle rejected", email.Id, email.CampaignId);
            return CommandResult.Failed(new BluQubeErrorData(EmailInboxErrorCodes.EmailIsPartOfCampaign, "Email is part of a campaign and cannot be favorited or unfavorited."));
        }

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