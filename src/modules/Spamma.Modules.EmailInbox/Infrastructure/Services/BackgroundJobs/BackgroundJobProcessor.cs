using System.Threading.Channels;
using BluQube.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job processor for recording campaign captures and chaos address events.
/// Reads from channels and dispatches commands with fresh scopes to avoid ObjectDisposedException.
/// </summary>
public sealed class BackgroundJobProcessor(
    Channel<CampaignCaptureJob> campaignChannel,
    Channel<ChaosAddressReceivedJob> chaosChannel,
    IServiceProvider serviceProvider,
    ILogger<BackgroundJobProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run both job processors concurrently
        await Task.WhenAll(
            this.ProcessCampaignJobsAsync(stoppingToken),
            this.ProcessChaosJobsAsync(stoppingToken));
    }

    private async Task ProcessCampaignJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in campaignChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var commander = scope.ServiceProvider.GetRequiredService<ICommander>();

                    await commander.Send(
                        new RecordCampaignCaptureCommand(
                            job.SubdomainId,
                            job.EmailId,
                            job.CampaignValue,
                            job.CapturedAt),
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process campaign capture job for email {EmailId}", job.EmailId);

                    // Continue processing other jobs even if one fails
                }
            }
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Campaign job processor stopped");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Campaign job processor encountered unrecoverable error");
        }
    }

    private async Task ProcessChaosJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in chaosChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var commander = scope.ServiceProvider.GetRequiredService<ICommander>();

                    await commander.Send(
                        new RecordChaosAddressReceivedCommand(job.ChaosAddressId, job.ReceivedAt),
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process chaos address job for {ChaosAddressId}", job.ChaosAddressId);

                    // Continue processing other jobs even if one fails
                }
            }
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Chaos address job processor stopped");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Chaos address job processor encountered unrecoverable error");
        }
    }
}
