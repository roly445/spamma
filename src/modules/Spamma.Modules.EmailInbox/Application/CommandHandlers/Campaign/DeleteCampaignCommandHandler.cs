using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

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
        var campaignId = GuidFromCampaignValue(request.SubdomainId, request.CampaignValue);
        var campaignMaybe = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        if (campaignMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Campaign '{campaignId}' not found"));
        }

        var campaign = campaignMaybe.Value;
        var deleteResult = campaign.Delete(timeProvider.GetUtcNow(), request.Force);

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

    private static Guid GuidFromCampaignValue(Guid subdomainId, string campaignValue)
    {
        // Create deterministic GUID from subdomain ID + campaign value
        var combined = subdomainId.ToString() + ":" + campaignValue;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hash.Take(16).ToArray());
    }
}
