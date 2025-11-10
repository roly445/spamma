using JasperFx.Events;
using Marten;
using Marten.Events.Projections;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

/// <summary>
/// Projection that handles the deletion of emails when a campaign is deleted.
/// This runs asynchronously in the background via Marten's async daemon.
/// </summary>
public class CampaignDeletionProjection : EventProjection
{
    public CampaignDeletionProjection()
    {
        Name = "CampaignDeletion";
    }

    public static async Task Project(IEvent<CampaignDeleted> campaignEvent, IDocumentOperations ops)
    {
        var campaignId = campaignEvent.StreamId;
        var deletedAt = campaignEvent.Data.DeletedAt;

        // Open a new session for querying
        await using var querySession = ops.DocumentStore.LightweightSession();

        // Find all emails for this campaign that haven't been deleted yet
        var emailIds = await querySession.Query<EmailLookup>()
            .Where(e => e.CampaignId == campaignId && e.WhenDeleted == null)
            .Select(e => e.Id)
            .ToListAsync();

        // Append EmailDeleted events for each email
        foreach (var emailId in emailIds)
        {
            ops.Events.Append(emailId, new EmailDeleted(deletedAt.DateTime));
        }
    }
}
