using System;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

public partial class ChaosAddress
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case ChaosAddressCreated e:
                this.Apply(e);
                break;
            case ChaosAddressEnabled e:
                this.Apply(e);
                break;
            case ChaosAddressDisabled e:
                this.Apply(e);
                break;
            case ChaosAddressReceived e:
                this.Apply(e);
                break;
            case ChaosAddressDeleted e:
                this.Apply(e);
                break;
            case ChaosAddressLocalPartChanged e:
                this.Apply(e);
                break;
            case ChaosAddressSubdomainChanged e:
                this.Apply(e);
                break;
            case ChaosAddressSmtpCodeChanged e:
                this.Apply(e);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(ChaosAddressCreated @event)
    {
        _ = @event;
        this.Id = @event.Id;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.LocalPart = @event.LocalPart;
        this.ConfiguredSmtpCode = @event.ConfiguredSmtpCode;
        this.Enabled = false;
        this.TotalReceived = 0;
        this.LastReceivedAt = null;
    }

    private void Apply(ChaosAddressEnabled @event)
    {
        this._suspensionAudits.Add(ChaosAddressSuspensionAudit.CreateSuspension(@event.When));
        this.Enabled = true;
    }

    private void Apply(ChaosAddressDisabled @event)
    {
        this._suspensionAudits.Add(ChaosAddressSuspensionAudit.CreateUnsuspension(@event.When));
        this.Enabled = false;
    }

    private void Apply(ChaosAddressReceived @event)
    {
        this.TotalReceived += 1;
        this.LastReceivedAt = @event.When;
    }

    private void Apply(ChaosAddressDeleted @event)
    {
        _ = @event;
    }

    private void Apply(ChaosAddressLocalPartChanged @event)
    {
        this.LocalPart = @event.NewLocalPart;
    }

    private void Apply(ChaosAddressSubdomainChanged @event)
    {
        this.DomainId = @event.NewDomainId;
        this.SubdomainId = @event.NewSubdomainId;
    }

    private void Apply(ChaosAddressSmtpCodeChanged @event)
    {
        this.ConfiguredSmtpCode = @event.NewSmtpCode;
    }
}
