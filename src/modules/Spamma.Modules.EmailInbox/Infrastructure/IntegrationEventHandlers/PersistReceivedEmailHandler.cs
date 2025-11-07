using BluQube.Commands;
using BluQube.Constants;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Infrastructure.IntegrationEventHandlers;

public class PersistReceivedEmailHandler(
    ILogger<PersistReceivedEmailHandler> logger,
    ICommander commander) : ICapSubscribe
{
    [CapSubscribe(nameof(IntegrationEventNames.EmailReceived))]
    public async Task OnEmailReceived(EmailReceivedIntegrationEvent ev)
    {
        try
        {
            logger.LogDebug("Persisting received email {EmailId} for subdomain {SubdomainId}", ev.EmailId, ev.SubdomainId);

            var addresses = ev.Recipients
                .Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.DisplayName ?? string.Empty, (EmailAddressType)x.Type))
                .ToList();

            var saveResult = await commander.Send(
                new ReceivedEmailCommand(
                    ev.EmailId,
                    ev.DomainId,
                    ev.SubdomainId,
                    ev.Subject ?? string.Empty,
                    ev.ReceivedAt,
                    addresses),
                CancellationToken.None);

            if (saveResult.Status == CommandResultStatus.Failed)
            {
                logger.LogError("Failed to persist email {EmailId} to database: {Status}", ev.EmailId, saveResult.Status);
                throw new InvalidOperationException($"Failed to persist email {ev.EmailId}");
            }

            // Record chaos address receive if applicable
            if (ev.ChaosAddressId.HasValue)
            {
                try
                {
                    await commander.Send(
                        new RecordChaosAddressReceivedCommand(ev.ChaosAddressId.Value, DateTime.UtcNow),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to record chaos address receive for {ChaosAddressId}", ev.ChaosAddressId);
                }
            }

            // Record campaign capture if applicable
            if (!string.IsNullOrEmpty(ev.CampaignValue))
            {
                try
                {
                    await commander.Send(
                        new RecordCampaignCaptureCommand(
                            ev.SubdomainId,
                            ev.EmailId,
                            ev.CampaignValue,
                            DateTimeOffset.UtcNow),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to record campaign capture for email {EmailId}", ev.EmailId);
                }
            }

            logger.LogDebug("Successfully persisted email {EmailId}", ev.EmailId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process received email {EmailId}", ev.EmailId);
            throw new InvalidOperationException($"Failed to process received email {ev.EmailId}", ex);
        }
    }
}
