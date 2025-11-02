using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Infrastructure.Projections;

public static class ChaosAddressLookupProjection
{
    public static ChaosAddressLookup Handle(ChaosAddressCreated @event)
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
        };
    }

    public static void Handle(ChaosAddressEnabled @event, ChaosAddressLookup readModel)
    {
        readModel.Enabled = true;
    }

    public static void Handle(ChaosAddressDisabled @event, ChaosAddressLookup readModel)
    {
        readModel.Enabled = false;
    }

    public static void Handle(ChaosAddressReceived @event, ChaosAddressLookup readModel)
    {
        readModel.TotalReceived += 1;
        readModel.LastReceivedAt = @event.When;
    }
}
