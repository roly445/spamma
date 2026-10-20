using Marten;
using Marten.Patching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public sealed class MessageSearchMetadataBackfillService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    ILogger<MessageSearchMetadataBackfillService> logger) : BackgroundService
{
    private const int BatchSize = 50;

    internal static async Task<int> BackfillBatchAsync(
        IDocumentSession session,
        IMessageStoreProvider messageStore,
        IHostEnvironment hostEnvironment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var emails = await session.Query<EmailLookup>()
            .Where(email => (email.SearchableBody == null || email.HasAttachment == null) && email.DeletedAt == null)
            .OrderBy(email => email.SentAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var failed = 0;
        foreach (var email in emails)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var emailId = email.Id;
            var filePath = Path.Combine(hostEnvironment.ContentRootPath, "messages", $"{emailId}.eml");
            var searchableBody = email.SearchableBody ?? string.Empty;
            var hasAttachment = email.HasAttachment ?? false;
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
                        if (email.SearchableBody is null)
                        {
                            searchableBody = MessageBodyTextExtractor.Extract(message.Value);
                        }

                        if (email.HasAttachment is null)
                        {
                            hasAttachment = message.Value.Attachments.Any();
                        }
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
                logger.LogWarning("Email {EmailId} search metadata backfill: {Error}", emailId, error);
            }

            session.Patch<EmailLookup>(emailId)
                .Set(email => email.SearchableBody, searchableBody)
                .Set(email => email.BodyIndexError, email.SearchableBody is null ? error : email.BodyIndexError)
                .Set(email => email.HasAttachment, hasAttachment)
                .Set(email => email.AttachmentIndexError, email.HasAttachment is null ? error : email.AttachmentIndexError);
        }

        if (emails.Count > 0)
        {
            await session.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Search metadata backfilled {ProcessedCount} messages; {FailedCount} had missing or unreadable content",
                emails.Count, failed);
        }

        return emails.Count;
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
                logger.LogError(exception, "Message search metadata backfill failed; retrying later");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
