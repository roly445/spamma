using JetBrains.Annotations;
using Marten.Events.Projections;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Infrastructure.Projections;

public class ChaosAddressLookupProjection : EventProjection
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
            ImmutableAfterFirstReceive = false,
            TotalReceived = 0,
            LastReceivedAt = null,
            CreatedAt = @event.CreatedAt,
            CreatedBy = Guid.Empty,
        };
    }

    [UsedImplicitly]
    public void Project(ChaosAddressEnabled @event, ChaosAddressLookup readModel)
    {
        readModel.Enabled = true;
    }

    [UsedImplicitly]
    public void Project(ChaosAddressDisabled @event, ChaosAddressLookup readModel)
    {
        readModel.Enabled = false;
    }

    [UsedImplicitly]
    public void Project(ChaosAddressReceived @event, ChaosAddressLookup readModel)
    {
        readModel.TotalReceived += 1;
        readModel.LastReceivedAt = @event.When;
    }
}
