using System.Threading.Channels;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background service that processes email ingestion jobs: stores files and executes ReceivedEmailCommand.
/// </summary>
public sealed class EmailIngestionProcessor(
    Channel<EmailIngestionJob> ingestionChannel,
    Channel<CampaignCaptureJob> campaignChannel,
    Channel<ChaosAddressReceivedJob> chaosChannel,
    IServiceProvider serviceProvider,
    ILogger<EmailIngestionProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email ingestion processor started");

        try
        {
            await foreach (var job in ingestionChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessEmailIngestionJobAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process email ingestion job for message {MessageId}", job.MessageId);

                    // Continue processing other jobs even if one fails
                }
            }
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Email ingestion processor stopped");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Email ingestion processor encountered unrecoverable error");
        }
    }

    private static Guid GuidFromCampaignValue(Guid subdomainId, string campaignValue)
    {
        // Create deterministic GUID from subdomain ID + campaign value
        // Same logic as RecordCampaignCaptureCommandHandler
        var combined = subdomainId.ToString() + ":" + campaignValue;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hash.Take(16).ToArray());
    }

    private async Task ProcessEmailIngestionJobAsync(EmailIngestionJob job, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var commander = scope.ServiceProvider.GetRequiredService<ICommander>();
        var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<Application.Repositories.ICampaignRepository>();

        // Campaign optimization: For campaign emails, we use lightweight processing
        // BUT we need to save the FIRST email as a sample
        if (job.IsCampaignEmail)
        {
            logger.LogDebug("Campaign email {MessageId} - checking if first email (sample)", job.MessageId);

            // Check if this is the first email in the campaign (by checking if Campaign exists)
            var campaignHeader = job.Message.Headers.FirstOrDefault(x => x.Field.Equals("x-spamma-camp", StringComparison.InvariantCultureIgnoreCase));
            var campaignValue = campaignHeader?.Value?.Trim().ToLowerInvariant() ?? string.Empty;
            var campaignId = GuidFromCampaignValue(job.SubdomainId, campaignValue);

            var campaignMaybe = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);

            if (campaignMaybe.HasNoValue)
            {
                // This is the FIRST email in the campaign - save it as the sample
                logger.LogInformation("First campaign email detected for {CampaignValue}, storing as sample", campaignValue);

                var saveFileResult = await messageStoreProvider.StoreMessageContentAsync(job.MessageId, job.Message, cancellationToken);
                if (!saveFileResult.IsSuccess)
                {
                    logger.LogError("Failed to store sample file for campaign {CampaignValue}", campaignValue);

                    // Don't fail completely - still queue the campaign job
                }
            }
            else
            {
                logger.LogDebug("Subsequent campaign email for {CampaignValue}, skipping file storage", campaignValue);
            }

            // Queue campaign capture - this creates/updates the Campaign aggregate
            await QueueCampaignCaptureJobAsync(job, cancellationToken);

            return;
        }

        // For non-campaign emails: File already stored synchronously in SpammaMessageStore
        // Here we just execute the command and queue downstream jobs

        // Step 1: Build email addresses
        var addresses = job.Message.To.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.To)).ToList();
        addresses.AddRange(job.Message.Cc.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.Cc)));
        addresses.AddRange(job.Message.Bcc.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.Bcc)));
        addresses.AddRange(job.Message.From.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.From)));

        // Step 2: Execute ReceivedEmailCommand (DB write - can be retried if this fails)
        var saveDataResult = await commander.Send(
            new ReceivedEmailCommand(
                job.MessageId,
                job.ParentDomainId,
                job.SubdomainId,
                job.Message.Subject,
                job.Message.Date.DateTime,
                addresses),
            cancellationToken);

        if (saveDataResult.Status == CommandResultStatus.Failed)
        {
            logger.LogError("ReceivedEmailCommand failed for {MessageId}, but file is safe on disk", job.MessageId);

            // NOTE: File remains on disk - recovery service will retry this on next startup
            return;
        }

        // Step 3: Queue downstream jobs (chaos address tracking)
        await QueueChaosAddressJobAsync(job, cancellationToken);

        logger.LogDebug("Successfully processed email ingestion job for {MessageId}", job.MessageId);
    }

    private async Task QueueCampaignCaptureJobAsync(EmailIngestionJob job, CancellationToken cancellationToken)
    {
        var campaignHeader = job.Message.Headers.FirstOrDefault(x => x.Field.Equals("x-spamma-camp", StringComparison.InvariantCultureIgnoreCase));
        if (campaignHeader == null || string.IsNullOrEmpty(campaignHeader.Value))
        {
            return;
        }

        var campaignValue = campaignHeader.Value.Trim().ToLowerInvariant();
        var fromAddress = job.Message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown";
        var toAddress = job.Message.To.Mailboxes.FirstOrDefault()?.Address ?? "unknown";

        var campaignJob = new CampaignCaptureJob(
            job.ParentDomainId,
            job.SubdomainId,
            job.MessageId,
            campaignValue,
            job.Message.Subject ?? string.Empty,
            fromAddress,
            toAddress,
            DateTimeOffset.UtcNow);

        try
        {
            await campaignChannel.Writer.WriteAsync(campaignJob, cancellationToken);
        }
        catch (ChannelClosedException ex)
        {
            logger.LogWarning(ex, "Campaign job channel closed, background processor may have stopped");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to queue campaign capture job for email {EmailId}", job.MessageId);
        }
    }

    private async Task QueueChaosAddressJobAsync(EmailIngestionJob job, CancellationToken cancellationToken)
    {
        if (!job.ChaosAddressId.HasValue)
        {
            return;
        }

        var chaosJob = new ChaosAddressReceivedJob(job.ChaosAddressId.Value, DateTime.UtcNow);

        try
        {
            await chaosChannel.Writer.WriteAsync(chaosJob, cancellationToken);
        }
        catch (ChannelClosedException ex)
        {
            logger.LogWarning(ex, "Chaos address job channel closed, background processor may have stopped");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to queue chaos address job for {ChaosAddressId}", job.ChaosAddressId);
        }
    }
}
