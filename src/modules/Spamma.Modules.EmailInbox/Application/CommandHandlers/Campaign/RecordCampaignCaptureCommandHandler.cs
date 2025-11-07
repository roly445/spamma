using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;

public class RecordCampaignCaptureCommandHandler(
    IEnumerable<IValidator<RecordCampaignCaptureCommand>> validators,
    ILogger<RecordCampaignCaptureCommandHandler> logger,
    ICampaignRepository campaignRepository)
    : CommandHandler<RecordCampaignCaptureCommand>(validators, logger)
{
    private static readonly Dictionary<Guid, SemaphoreSlim> CampaignLocks = new();
    private static readonly object LockDictionaryLock = new();

    protected override async Task<CommandResult> HandleInternal(RecordCampaignCaptureCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var campaignId = GuidFromCampaignValue(request.SubdomainId, request.CampaignValue);

            var @lock = GetOrCreateLock(campaignId);
            await @lock.WaitAsync(cancellationToken);

            try
            {
                var campaignMaybe = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);

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
                        return CommandResult.Failed(createResult.Error);
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
                        return CommandResult.Failed(captureResult.Error);
                    }
                }

                // Save campaign aggregate
                var saveResult = await campaignRepository.SaveAsync(campaign, cancellationToken);
                if (!saveResult.IsSuccess)
                {
                    return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
                }

                return CommandResult.Succeeded();
            }
            finally
            {
                @lock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record campaign capture for campaign {CampaignValue}", request.CampaignValue);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }
    }

    private static SemaphoreSlim GetOrCreateLock(Guid campaignId)
    {
        lock (LockDictionaryLock)
        {
            if (!CampaignLocks.TryGetValue(campaignId, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                CampaignLocks[campaignId] = semaphore;
            }

            return semaphore;
        }
    }

    private static Guid GuidFromCampaignValue(Guid subdomainId, string campaignValue)
    {
        // Create deterministic GUID from subdomain ID + campaign value
        var combined = subdomainId.ToString() + ":" + campaignValue;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hash.Take(16).ToArray());
    }
}
