using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

public class CampaignSummaryProjection : EventProjection
{
    [UsedImplicitly]
    public CampaignSummary Create(CampaignCaptured @event)
    {
        return new CampaignSummary
        {
            CampaignId = @event.CampaignId,
            SubdomainId = @event.SubdomainId,
            CampaignValue = @event.CampaignValue,
            FirstReceivedAt = @event.CapturedAt,
            LastReceivedAt = @event.CapturedAt,
            TotalCaptured = 1,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignCaptured> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.TotalCaptured)
            .Set(x => x.LastReceivedAt, @event.Data.CapturedAt);
    }
}
