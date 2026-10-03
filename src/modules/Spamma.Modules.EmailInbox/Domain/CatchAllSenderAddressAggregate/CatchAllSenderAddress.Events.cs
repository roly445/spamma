using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

public partial class CatchAllSenderAddress
{
    internal static CatchAllSenderAddress Replay(CatchAllSenderAddress? aggregate, object @event)
    {
        if (aggregate is null && @event is not CatchAllSenderAddressAdded)
        {
            throw new ArgumentException("The first event must be CatchAllSenderAddressAdded.", nameof(@event));
        }

        aggregate ??= new CatchAllSenderAddress();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case CatchAllSenderAddressAdded added:
                this.ApplyRecorded(added);
                break;
            case CatchAllSenderAddressRemoved removed:
                this.ApplyRecorded(removed);
                break;
            case UserAssignedToCatchAllSender assigned:
                this.ApplyRecorded(assigned);
                break;
            case UserUnassignedFromCatchAllSender unassigned:
                this.ApplyRecorded(unassigned);
                break;
        }
    }

    private void ApplyRecorded(CatchAllSenderAddressAdded @event)
    {
        this.Id = @event.AddressId;
        this._senderAddress = @event.SenderAddress;
    }

    private void ApplyRecorded(CatchAllSenderAddressRemoved unused)
    {
        _ = unused;
        this._isRemoved = true;
    }

    private void ApplyRecorded(UserAssignedToCatchAllSender @event)
    {
        this._assignedUserIds.Add(@event.UserId);
    }

    private void ApplyRecorded(UserUnassignedFromCatchAllSender @event)
    {
        this._assignedUserIds.Remove(@event.UserId);
    }
}
