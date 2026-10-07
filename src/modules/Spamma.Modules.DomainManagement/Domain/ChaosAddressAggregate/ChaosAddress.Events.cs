using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

/// <summary>
/// Event handling for <see cref="ChaosAddress"/> aggregate.
/// </summary>
public partial class ChaosAddress
{
    internal static ChaosAddress Replay(ChaosAddress? aggregate, object @event)
    {
        if (aggregate is null && @event is not ChaosAddressCreated)
        {
            throw new ArgumentException("The first event must be ChaosAddressCreated.", nameof(@event));
        }

        aggregate ??= new ChaosAddress();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case ChaosAddressCreated e:
                this.ApplyRecorded(e);
                break;
            case ChaosSpamReportingSelected:
                this.ReportsSpam = true;
                break;
            case ChaosAddressEnabled e:
                this.ApplyRecorded(e);
                break;
            case ChaosAddressDisabled e:
                this.ApplyRecorded(e);
                break;
            case ChaosAddressReceived e:
                this.ApplyRecorded(e);
                break;
            case ChaosAddressReceivedV2 e:
                this.TotalReceived += 1;
                this._lastReceivedAt = e.ReceivedAt;
                this._receivedAttemptIds.Add(e.AttemptId);
                break;
            case ChaosAddressDeleted e:
                ApplyRecorded(e);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private static void ApplyRecorded(ChaosAddressDeleted @event)
    {
        _ = @event;
    }

    private void ApplyRecorded(ChaosAddressCreated @event)
    {
        _ = @event;
        this.Id = @event.Id;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.LocalPart = @event.LocalPart;
        this.ConfiguredSmtpCode = @event.ConfiguredSmtpCode;
        this.ReportsSpam = false;
        this.Enabled = false;
        this.TotalReceived = 0;
        this._lastReceivedAt = null;
    }

    private void ApplyRecorded(ChaosAddressEnabled @event)
    {
        this._suspensionAudits.Add(ChaosAddressSuspensionAudit.CreateSuspension(@event.EnabledAt));
        this.Enabled = true;
    }

    private void ApplyRecorded(ChaosAddressDisabled @event)
    {
        this._suspensionAudits.Add(ChaosAddressSuspensionAudit.CreateUnsuspension(@event.DisabledAt));
        this.Enabled = false;
    }

    private void ApplyRecorded(ChaosAddressReceived @event)
    {
        this.TotalReceived += 1;
        this._lastReceivedAt = @event.ReceivedAt;
    }
}
