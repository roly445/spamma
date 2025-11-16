using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

public class CampaignSummaryProjection : EventProjection
{
    [UsedImplicitly]
    public CampaignSummary Create(CampaignCreated @event)
    {
        return new CampaignSummary
        {
            CampaignId = @event.CampaignId,
            DomainId = @event.DomainId,
            SubdomainId = @event.SubdomainId,
            CampaignValue = @event.CampaignValue,
            SampleMessageId = @event.MessageId,
            FirstReceivedAt = @event.CreatedAt,
            LastReceivedAt = @event.CreatedAt,
            TotalCaptured = 1,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<Domain.CampaignAggregate.Events.CampaignCaptured> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.TotalCaptured)
            .Set(x => x.LastReceivedAt, @event.Data.CapturedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<Domain.CampaignAggregate.Events.CampaignDeleted> @event, IDocumentOperations ops)
    {
        ops.Delete<CampaignSummary>(@event.StreamId);
    }
}