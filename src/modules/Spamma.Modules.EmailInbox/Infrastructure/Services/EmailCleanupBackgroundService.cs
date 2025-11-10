using BluQube.Commands;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
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
        var commander = scope.ServiceProvider.GetRequiredService<ICommander>();

        var cutoffDate = timeProvider.GetUtcNow().DateTime.Subtract(EmailRetentionPeriod);

        logger.LogDebug("Starting email cleanup for emails older than {CutoffDate}", cutoffDate);

        // Find emails that are older than 24 hours, not already deleted, and not marked as favorite
        var oldEmails = await documentSession
            .Query<EmailLookup>()
            .Where(e => e.WhenSent < cutoffDate && e.WhenDeleted == null && !e.IsFavorite)
            .Take(100) // Process in batches to avoid overwhelming the system
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
                var deleteCommand = new DeleteEmailCommand(email.Id);
                var result = await commander.Send(deleteCommand, cancellationToken);

                // Check if the command succeeded (pattern from other handlers)
                if (result != null)
                {
                    deletedCount++;
                    logger.LogDebug(
                        "Successfully deleted email {EmailId} from {WhenSent}",
                        email.Id, email.WhenSent);
                }
                else
                {
                    failedCount++;
                    logger.LogWarning(
                        "Failed to delete email {EmailId}: Command returned null result",
                        email.Id);
                }
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