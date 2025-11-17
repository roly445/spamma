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
        return new CampaignSummary();
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignCreated> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Set(x => x.CampaignId, @event.StreamId)
            .Set(x => x.DomainId, @event.Data.DomainId)
            .Set(x => x.SubdomainId, @event.Data.SubdomainId)
            .Set(x => x.CampaignValue, @event.Data.CampaignValue)
            .Set(x => x.SampleMessageId, @event.Data.MessageId)
            .Set(x => x.FirstReceivedAt, @event.Data.CreatedAt)
            .Set(x => x.LastReceivedAt, @event.Data.CreatedAt)
            .Set(x => x.TotalCaptured, 1);
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignCaptured> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.TotalCaptured)
            .Set(x => x.LastReceivedAt, @event.Data.CapturedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignDeleted> @event, IDocumentOperations ops)
    {
        ops.Delete<CampaignSummary>(@event.StreamId);
    }
}