using Marten;
using Marten.Patching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public sealed class MessageBodySearchBackfillService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    ILogger<MessageBodySearchBackfillService> logger) : BackgroundService
{
    private const int BatchSize = 50;

    internal static async Task<int> BackfillBatchAsync(
        IDocumentSession session,
        IMessageStoreProvider messageStore,
        IHostEnvironment hostEnvironment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var emailIds = await session.Query<EmailLookup>()
            .Where(email => email.SearchableBody == null && email.DeletedAt == null)
            .OrderBy(email => email.SentAt)
            .Take(BatchSize)
            .Select(email => email.Id)
            .ToListAsync(cancellationToken);

        var failed = 0;
        foreach (var emailId in emailIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = Path.Combine(hostEnvironment.ContentRootPath, "messages", $"{emailId}.eml");
            var searchableBody = string.Empty;
            string? error = null;

            if (!File.Exists(filePath))
            {
                error = "Stored EML is missing";
            }
            else
            {
                try
                {
                    var message = await messageStore.LoadMessageContentAsync(emailId, cancellationToken);
                    if (message.HasValue)
                    {
                        searchableBody = MessageBodyTextExtractor.Extract(message.Value);
                    }
                    else
                    {
                        error = "Stored EML could not be parsed or read";
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
                {
                    error = "Search text could not be extracted";
                    logger.LogWarning(exception, "Could not backfill searchable body for email {EmailId}", emailId);
                }
            }

            if (error is not null)
            {
                failed++;
                logger.LogWarning("Email {EmailId} body search backfill: {Error}", emailId, error);
            }

            session.Patch<EmailLookup>(emailId)
                .Set(email => email.SearchableBody, searchableBody)
                .Set(email => email.BodyIndexError, error);
        }

        if (emailIds.Count > 0)
        {
            await session.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Body search backfilled {ProcessedCount} messages; {FailedCount} had missing or unreadable content",
                emailIds.Count, failed);
        }

        return emailIds.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
                var messageStore = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
                var processed = await BackfillBatchAsync(session, messageStore, hostEnvironment, logger, stoppingToken);
                await Task.Delay(processed == 0 ? TimeSpan.FromHours(6) : TimeSpan.FromMilliseconds(250), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Message body search backfill failed; retrying later");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
