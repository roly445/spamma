using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

public partial class CampaignSummaryProjection : EventProjection
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
    public CampaignSummary Create(CampaignObservedViaFailure @event) => new()
    {
        CampaignId = @event.CampaignId,
        DomainId = @event.DomainId,
        SubdomainId = @event.SubdomainId,
        CampaignValue = @event.CampaignValue,
        FirstReceivedAt = @event.ObservedAt,
        LastReceivedAt = @event.ObservedAt,
    };

    [UsedImplicitly]
    public void Project(IEvent<CampaignFirstCaptured> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.TotalCaptured)
            .Set(x => x.SampleMessageId, @event.Data.MessageId)
            .Set(x => x.LastReceivedAt, @event.Data.CapturedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignDeliveryFailureRecorded> @event, IDocumentOperations ops)
    {
        var patch = ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.FailureDeliveryAttempts)
            .Set(x => x.LastReceivedAt, @event.Data.ObservedAt);
        if (@event.Data.SmtpCode < 500)
        {
            patch.Increment(x => x.TemporaryFailureMessages);
        }
        else
        {
            patch.Increment(x => x.PermanentFailureMessages);
        }
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignDeliveryFailureRetried> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.FailureDeliveryAttempts)
            .Set(x => x.LastReceivedAt, @event.Data.ObservedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignCaptured> @event, IDocumentOperations ops)
    {
        ops.Patch<CampaignSummary>(@event.StreamId)
            .Increment(x => x.TotalCaptured)
            .Set(x => x.LastReceivedAt, @event.Data.CapturedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<CampaignCapturedV2> @event, IDocumentOperations ops)
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
