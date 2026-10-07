using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed class SpamFeedbackDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<SpamFeedbackDeliveryWorker> logger) : BackgroundService
{
    private const int MaximumAutomaticAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.ProcessDueReportsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Spam feedback delivery poll failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private static bool IsDue(FeedbackChannelState channel, DateTimeOffset now) =>
        channel.Status is (FeedbackDeliveryStatus.Pending or FeedbackDeliveryStatus.Failed) &&
        channel.NextAttemptAt <= now;

    private async Task ProcessDueReportsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var now = clock.GetUtcNow();
        var dueIds = await session.Query<SpamReport>()
            .Where(report => report.NextAttemptAt != null && report.NextAttemptAt <= now)
            .OrderBy(report => report.NextAttemptAt)
            .Take(20)
            .Select(report => report.Id)
            .ToListAsync(cancellationToken);

        foreach (var reportId in dueIds)
        {
            await this.ProcessOneAsync(reportId, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The spam feedback worker requires the application database.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var lockKey = BitConverter.ToInt64(reportId.ToByteArray());
        await using var lockCommand = new NpgsqlCommand("select pg_try_advisory_lock(@key)", connection);
        lockCommand.Parameters.AddWithValue("key", lockKey);
        if (await lockCommand.ExecuteScalarAsync(cancellationToken) is not true)
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
            var report = await session.LoadAsync<SpamReport>(reportId, cancellationToken);
            if (report is null || report.NextAttemptAt > clock.GetUtcNow())
            {
                return;
            }

            var now = clock.GetUtcNow();
            if (IsDue(report.EmailDelivery, now))
            {
                report = report with
                {
                    EmailDelivery = await this.AttemptAsync(report.EmailDelivery, now, async () =>
                    {
                        var store = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
                        var original = await store.LoadMessageContentAsync(reportId, cancellationToken);
                        if (original.HasNoValue)
                        {
                            throw new InvalidOperationException("The captured message content is unavailable.");
                        }

                        await scope.ServiceProvider.GetRequiredService<IArfFeedbackSender>()
                            .SendAsync(report, original.Value, cancellationToken);
                    }, cancellationToken),
                };
            }

            if (IsDue(report.WebhookDelivery, now))
            {
                report = report with
                {
                    WebhookDelivery = await this.AttemptAsync(report.WebhookDelivery, now, () =>
                        scope.ServiceProvider.GetRequiredService<WebhookFeedbackSender>()
                            .SendAsync(report, cancellationToken), cancellationToken),
                };
            }

            var next = new[] { report.EmailDelivery.NextAttemptAt, report.WebhookDelivery.NextAttemptAt }
                .Where(value => value.HasValue)
                .Min();
            session.Store(report with { NextAttemptAt = next });
            await session.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            await using var unlock = new NpgsqlCommand("select pg_advisory_unlock(@key)", connection);
            unlock.Parameters.AddWithValue("key", lockKey);
            await unlock.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    private async Task<FeedbackChannelState> AttemptAsync(
        FeedbackChannelState previous,
        DateTimeOffset now,
        Func<Task> send,
        CancellationToken cancellationToken)
    {
        var attempts = previous.Attempts + 1;
        try
        {
            await send();
            return new FeedbackChannelState(FeedbackDeliveryStatus.Succeeded, attempts, DeliveredAt: now);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Spam feedback delivery attempt {Attempt} failed", attempts);
            var next = attempts < MaximumAutomaticAttempts
                ? now.AddMinutes(Math.Pow(2, attempts - 1))
                : (DateTimeOffset?)null;
            return new FeedbackChannelState(
                FeedbackDeliveryStatus.Failed,
                attempts,
                ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                NextAttemptAt: next);
        }
    }
}
