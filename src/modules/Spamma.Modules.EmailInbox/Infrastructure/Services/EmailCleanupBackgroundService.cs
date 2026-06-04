using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class EmailCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<EmailCleanupBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan EmailRetentionPeriod = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email cleanup background service started. Will check for old emails every {CheckInterval}", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.CleanupOldEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during email cleanup process");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        logger.LogInformation("Email cleanup background service stopped");
    }

    private async Task CleanupOldEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var cutoffDate = timeProvider.GetUtcNow().Subtract(EmailRetentionPeriod);

        logger.LogDebug("Starting email cleanup for emails older than {CutoffDate}", cutoffDate);

        // Campaign emails have their own lifecycle and are intentionally skipped here.
        var oldEmails = await documentSession
            .Query<EmailLookup>()
            .Where(e => e.SentAt < cutoffDate && e.DeletedAt == null && !e.IsFavorite && e.CampaignId == null)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (oldEmails.Count == 0)
        {
            logger.LogDebug("No old emails found for cleanup");
            return;
        }

        logger.LogInformation(
            "Found {EmailCount} non-favorite emails older than {RetentionPeriod} hours for cleanup",
            oldEmails.Count, EmailRetentionPeriod.TotalHours);

        var deletedCount = 0;
        var failedCount = 0;

        foreach (var email in oldEmails)
        {
            try
            {
                var emailMaybe = await emailRepository.GetByIdAsync(email.Id, cancellationToken);
                if (emailMaybe.HasNoValue)
                {
                    failedCount++;
                    logger.LogWarning("Failed to delete email {EmailId}: aggregate was not found", email.Id);
                    continue;
                }

                var emailAggregate = emailMaybe.Value;
                if (emailAggregate.IsFavorite || emailAggregate.IsPartOfCampaign)
                {
                    failedCount++;
                    logger.LogWarning(
                        "Skipping email {EmailId} during cleanup because it is favorite or part of a campaign",
                        email.Id);
                    continue;
                }

                var result = emailAggregate.Delete(timeProvider.GetUtcNow().DateTime);
                if (!result.IsSuccess)
                {
                    failedCount++;
                    logger.LogWarning(
                        "Failed to delete email {EmailId}: {ErrorCode} {ErrorMessage}",
                        email.Id,
                        result.Error?.Code,
                        result.Error?.Message);
                    continue;
                }

                var saveResult = await emailRepository.SaveAsync(emailAggregate, cancellationToken);
                if (!saveResult.IsSuccess)
                {
                    failedCount++;
                    logger.LogWarning("Failed to delete email {EmailId}: unable to save aggregate changes", email.Id);
                    continue;
                }

                await eventPublisher.PublishAsync(new EmailDeletedIntegrationEvent(email.Id), cancellationToken);

                deletedCount++;
                logger.LogDebug(
                    "Successfully deleted email {EmailId} from {WhenSent}",
                    email.Id, email.SentAt);
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogError(ex, "Exception occurred while deleting email {EmailId}", email.Id);
            }
        }

        logger.LogInformation(
            "Email cleanup completed: {DeletedCount} deleted, {FailedCount} failed",
            deletedCount, failedCount);
    }
}
