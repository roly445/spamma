using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;

public class RecordCampaignCaptureCommandHandler(
    IEnumerable<IValidator<RecordCampaignCaptureCommand>> validators,
    ILogger<RecordCampaignCaptureCommandHandler> logger,
    ICampaignRepository campaignRepository)
    : CommandHandler<RecordCampaignCaptureCommand, RecordCampaignCaptureCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<RecordCampaignCaptureCommandResult>> HandleInternal(RecordCampaignCaptureCommand request, CancellationToken cancellationToken)
    {
        var campaignId = GuidFromCampaignValue(request.SubdomainId, request.CampaignValue);

        var campaignMaybe = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        var isFirstEmail = campaignMaybe.HasNoValue;
        Domain.CampaignAggregate.Campaign campaign;
        if (campaignMaybe.HasNoValue)
        {
            var createResult = Domain.CampaignAggregate.Campaign.Create(
                campaignId,
                request.DomainId,
                request.SubdomainId,
                request.CampaignValue,
                request.MessageId,
                request.ReceivedAt);

            if (createResult.IsFailure)
            {
                return CommandResult<RecordCampaignCaptureCommandResult>.Failed(createResult.Error);
            }

            campaign = createResult.Value;
        }
        else
        {
            // Record additional capture to existing campaign
            campaign = campaignMaybe.Value;
            var captureResult = campaign.RecordCapture(request.MessageId, request.ReceivedAt);

            if (captureResult.IsFailure)
            {
                return CommandResult<RecordCampaignCaptureCommandResult>.Failed(captureResult.Error);
            }
        }

            // Save campaign aggregate
        var saveResult = await campaignRepository.SaveAsync(campaign, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult<RecordCampaignCaptureCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult<RecordCampaignCaptureCommandResult>.Succeeded(new RecordCampaignCaptureCommandResult(campaignId, isFirstEmail));
    }

    private static Guid GuidFromCampaignValue(Guid subdomainId, string campaignValue)
    {
        // Create deterministic GUID from subdomain ID + campaign value
        var combined = subdomainId.ToString() + ":" + campaignValue;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hash.Take(16).ToArray());
    }
}
