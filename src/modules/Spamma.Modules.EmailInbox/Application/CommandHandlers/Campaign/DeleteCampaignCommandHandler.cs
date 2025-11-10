using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;

public class DeleteCampaignCommandHandler(
    ICampaignRepository campaignRepository,
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
        var deleteResult = campaign.Delete(timeProvider.GetUtcNow());

        if (deleteResult.IsFailure)
        {
            return CommandResult.Failed(deleteResult.Error);
        }

        var saveResult = await campaignRepository.SaveAsync(campaign, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}
