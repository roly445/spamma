using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

/// <summary>
/// Event handling for <see cref="ChaosAddress"/> aggregate.
/// </summary>
internal partial class ChaosAddress
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
                Apply(e);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private static void Apply(ChaosAddressDeleted @event)
    {
        _ = @event;
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
        this._lastReceivedAt = null;
    }

    private void Apply(ChaosAddressEnabled @event)
    {
        this._suspensionAudits.Add(ChaosAddressSuspensionAudit.CreateSuspension(@event.EnabledAt));
        this.Enabled = true;
    }

    private void Apply(ChaosAddressDisabled @event)
    {
        this._suspensionAudits.Add(ChaosAddressSuspensionAudit.CreateUnsuspension(@event.DisabledAt));
        this.Enabled = false;
    }

    private void Apply(ChaosAddressReceived @event)
    {
        this.TotalReceived += 1;
        this._lastReceivedAt = @event.ReceivedAt;
    }
}