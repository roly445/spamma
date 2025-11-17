using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;

internal class DeleteCampaignCommandHandler(
    ICampaignRepository campaignRepository,
    IEmailRepository emailRepository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<DeleteCampaignCommand>> validators,
    ILogger<DeleteCampaignCommandHandler> logger)
    : CommandHandler<DeleteCampaignCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaignMaybe = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);

        if (campaignMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Campaign '{request.CampaignId}' not found"));
        }

        var campaign = campaignMaybe.Value;
        var deleteResult = campaign.Delete(timeProvider.GetUtcNow().UtcDateTime);

        if (deleteResult.IsFailure)
        {
            return CommandResult.Failed(deleteResult.Error);
        }

        var saveResult = await campaignRepository.SaveAsync(campaign, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        // Delete all emails associated with this campaign
        var emails = await emailRepository.GetByCampaignIdAsync(campaign.Id, cancellationToken);
        var deletedAt = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var email in emails)
        {
            email.Delete(deletedAt);
            var emailSaveResult = await emailRepository.SaveAsync(email, cancellationToken);

            if (!emailSaveResult.IsSuccess)
            {
                logger.LogError(
                    "Failed to delete email {EmailId} when deleting campaign {CampaignId}",
                    email.Id,
                    campaign.Id);
                return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
            }
        }

        return CommandResult.Succeeded();
    }
}