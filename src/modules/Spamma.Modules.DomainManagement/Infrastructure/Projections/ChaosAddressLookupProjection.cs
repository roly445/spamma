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
        return new ChaosAddressLookup
        {
            Id = @event.Id,
            DomainId = @event.DomainId,
            SubdomainId = @event.SubdomainId,
            LocalPart = @event.LocalPart,
            ConfiguredSmtpCode = @event.ConfiguredSmtpCode,
            Enabled = false,
            TotalReceived = 0,
            LastReceivedAt = null,
            CreatedAt = @event.CreatedAt,
            CreatedBy = Guid.Empty,
        };
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