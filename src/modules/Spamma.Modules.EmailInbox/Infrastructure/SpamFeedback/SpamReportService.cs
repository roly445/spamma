using Marten;
using Npgsql;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed class SpamReportService(IDocumentSession session, IDocumentStore documentStore, IMessageStoreProvider messageStore, TimeProvider clock)
{
    public async Task<SpamReport?> CreateAsync(Guid emailId, SpamReportTrigger trigger, CancellationToken cancellationToken)
    {
        var existing = await session.LoadAsync<SpamReport>(emailId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var email = await session.LoadAsync<EmailLookup>(emailId, cancellationToken);
        if (email is null || email.DeletedAt.HasValue)
        {
            return null;
        }

        var configuration = await session.LoadAsync<SpamFeedbackConfiguration>(email.SubdomainId, cancellationToken);
        var original = await messageStore.LoadMessageContentAsync(emailId, cancellationToken);
        var campaignValue = original.HasValue ? original.Value.Headers["x-spamma-camp"] : email.CampaignValue;
        var now = clock.GetUtcNow();
        var arfEnabled = !string.IsNullOrWhiteSpace(configuration?.ArfRecipient);
        var webhookEnabled = !string.IsNullOrWhiteSpace(configuration?.WebhookUrl) &&
                             !string.IsNullOrWhiteSpace(configuration.ProtectedWebhookSecret);
        var report = new SpamReport(
            emailId,
            email.DomainId,
            email.SubdomainId,
            email.CampaignId,
            campaignValue,
            email.EmailAddresses.FirstOrDefault(x => x.EmailAddressType == EmailAddressType.To)?.Address ?? string.Empty,
            email.EmailAddresses.FirstOrDefault(x => x.EmailAddressType == EmailAddressType.From)?.Address ?? string.Empty,
            trigger,
            now,
            configuration?.ArfRecipient,
            configuration?.WebhookUrl,
            configuration?.ProtectedWebhookSecret,
            new FeedbackChannelState(
                arfEnabled ? FeedbackDeliveryStatus.Pending : FeedbackDeliveryStatus.Disabled,
                NextAttemptAt: arfEnabled ? now : null),
            new FeedbackChannelState(
                webhookEnabled ? FeedbackDeliveryStatus.Pending : FeedbackDeliveryStatus.Disabled,
                NextAttemptAt: webhookEnabled ? now : null),
            arfEnabled || webhookEnabled ? now : null);

        session.Insert(report);
        try
        {
            await session.SaveChangesAsync(cancellationToken);
            return report;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await using var readSession = documentStore.QuerySession();
            return await readSession.LoadAsync<SpamReport>(emailId, cancellationToken);
        }
    }
}
