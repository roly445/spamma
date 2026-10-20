using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Retention;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class EmailCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<EmailCleanupBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private const int MaxDeletesPerCycle = 100;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private int nextScopeIndex;

    internal async Task CleanupOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var policies = scope.ServiceProvider.GetRequiredService<EmailRetentionPolicyService>();

        // Campaigns follow their own lifecycle; favorites are always exempt. Both limits
        // count only ordinary, non-favorite messages within one subdomain inbox.
        var scopeRows = await documentSession
            .Query<EmailLookup>()
            .Where(e => e.DeletedAt == null && !e.IsFavorite && e.CampaignId == null)
            .Select(e => new { e.DomainId, e.SubdomainId })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (scopeRows.Count == 0)
        {
            return;
        }

        var scopes = scopeRows.OrderBy(x => x.DomainId).ThenBy(x => x.SubdomainId).ToArray();
        this.nextScopeIndex %= scopes.Length;

        var deletedCount = 0;
        var failedCount = 0;
        var visited = new HashSet<Guid>();
        var scannedScopes = 0;
        while (scannedScopes < scopes.Length && deletedCount + failedCount < MaxDeletesPerCycle)
        {
            var inbox = scopes[this.nextScopeIndex];
            this.nextScopeIndex = (this.nextScopeIndex + 1) % scopes.Length;
            scannedScopes++;
            var policy = await policies.GetEffectiveAsync(inbox.DomainId, inbox.SubdomainId, cancellationToken);
            var remaining = MaxDeletesPerCycle - deletedCount - failedCount;
            var candidates = new List<Guid>();
            var eligible = documentSession.Query<EmailLookup>()
                .Where(e => e.DomainId == inbox.DomainId && e.SubdomainId == inbox.SubdomainId &&
                    e.DeletedAt == null && !e.IsFavorite && e.CampaignId == null);

            if (policy.MaxAgeHours is int maxAgeHours)
            {
                var cutoff = timeProvider.GetUtcNow().AddHours(-maxAgeHours);
                var expired = await eligible.Where(e => e.SentAt < cutoff)
                    .OrderBy(e => e.SentAt).Take(remaining).Select(e => e.Id).ToListAsync(cancellationToken);
                candidates.AddRange(expired);
            }

            if (policy.MaxMessages is int maxMessages && candidates.Count < remaining)
            {
                var overLimit = await eligible.OrderByDescending(e => e.SentAt).ThenByDescending(e => e.Id)
                    .Skip(maxMessages).Take(remaining).Select(e => e.Id).ToListAsync(cancellationToken);
                candidates.AddRange(overLimit);
            }

            foreach (var emailId in candidates)
            {
                if (deletedCount + failedCount >= MaxDeletesPerCycle || !visited.Add(emailId))
                {
                    continue;
                }

                try
                {
                    var emailMaybe = await emailRepository.GetByIdAsync(emailId, cancellationToken);
                    if (emailMaybe.HasNoValue)
                    {
                        failedCount++;
                        logger.LogWarning("Failed to delete email {EmailId}: aggregate was not found", emailId);
                        continue;
                    }

                    var emailAggregate = emailMaybe.Value;
                    if (emailAggregate.IsFavorite || emailAggregate.IsPartOfCampaign)
                    {
                        failedCount++;
                        logger.LogWarning(
                            "Skipping email {EmailId} during cleanup because it is favorite or part of a campaign",
                            emailId);
                        continue;
                    }

                    var result = emailAggregate.Delete(timeProvider.GetUtcNow().DateTime);
                    if (!result.IsSuccess)
                    {
                        failedCount++;
                        logger.LogWarning(
                            "Failed to delete email {EmailId}: {ErrorCode} {ErrorMessage}",
                            emailId,
                            result.Error?.Code,
                            result.Error?.Message);
                        continue;
                    }

                    var saveResult = await emailRepository.SaveAsync(emailAggregate, cancellationToken);
                    if (!saveResult.IsSuccess)
                    {
                        failedCount++;
                        logger.LogWarning("Failed to delete email {EmailId}: unable to save aggregate changes", emailId);
                        continue;
                    }

                    await eventPublisher.PublishAsync(new EmailDeletedIntegrationEvent(emailId), cancellationToken);

                    deletedCount++;
                    logger.LogDebug("Deleted email {EmailId} under retention policy", emailId);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(ex, "Exception occurred while deleting email {EmailId}", emailId);
                }
            }
        }

        logger.LogInformation(
            "Email cleanup completed: {DeletedCount} deleted, {FailedCount} failed",
            deletedCount, failedCount);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email cleanup background service started. Will check for old emails every {CheckInterval}", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.CleanupOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during email cleanup process");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        logger.LogInformation("Email cleanup background service stopped");
    }
}
