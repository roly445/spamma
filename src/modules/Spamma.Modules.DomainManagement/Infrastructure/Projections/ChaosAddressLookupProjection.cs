using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Infrastructure.Projections;

internal class ChaosAddressLookupProjection : EventProjection
{
    [UsedImplicitly]
    public ChaosAddressLookup Create(ChaosAddressCreated @event)
    {
        return new ChaosAddressLookup();
    }

    [UsedImplicitly]
    public void Project(IEvent<ChaosAddressCreated> @event, IDocumentOperations ops)
    {
        ops.Patch<ChaosAddressLookup>(@event.StreamId)
            .Set(x => x.Id, @event.StreamId)
            .Set(x => x.DomainId, @event.Data.DomainId)
            .Set(x => x.SubdomainId, @event.Data.SubdomainId)
            .Set(x => x.LocalPart, @event.Data.LocalPart)
            .Set(x => x.ConfiguredSmtpCode, @event.Data.ConfiguredSmtpCode)
            .Set(x => x.Enabled, false)
            .Set(x => x.ImmutableAfterFirstReceive, false)
            .Set(x => x.TotalReceived, 0)
            .Set(x => x.LastReceivedAt, null)
            .Set(x => x.CreatedAt, @event.Data.CreatedAt)
            .Set(x => x.CreatedBy, Guid.Empty);
    }

    [UsedImplicitly]
    public void Project(IEvent<ChaosAddressEnabled> @event, IDocumentOperations ops)
    {
        ops.Patch<ChaosAddressLookup>(@event.StreamId)
            .Set(x => x.Enabled, true);
    }

    [UsedImplicitly]
    public void Project(IEvent<ChaosAddressDisabled> @event, IDocumentOperations ops)
    {
        ops.Patch<ChaosAddressLookup>(@event.StreamId)
            .Set(x => x.Enabled, false);
    }

    [UsedImplicitly]
    public void Project(IEvent<ChaosAddressReceived> @event, IDocumentOperations ops)
    {
        ops.Patch<ChaosAddressLookup>(@event.StreamId)
            .Increment(x => x.TotalReceived)
            .Set(x => x.LastReceivedAt, @event.Data.ReceivedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<ChaosAddressDeleted> @event, IDocumentOperations ops)
    {
        ops.Delete<ChaosAddressLookup>(@event.StreamId);
    }
}