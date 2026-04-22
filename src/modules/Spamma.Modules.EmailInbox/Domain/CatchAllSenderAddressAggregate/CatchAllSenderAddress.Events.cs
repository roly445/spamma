using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

public partial class CatchAllSenderAddress
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case CatchAllSenderAddressAdded added:
                this.Apply(added);
                break;
            case CatchAllSenderAddressRemoved removed:
                this.Apply(removed);
                break;
            case UserAssignedToCatchAllSender assigned:
                this.Apply(assigned);
                break;
            case UserUnassignedFromCatchAllSender unassigned:
                this.Apply(unassigned);
                break;
        }
    }

    private void Apply(CatchAllSenderAddressAdded @event)
    {
        this.Id = @event.AddressId;
        this._senderAddress = @event.SenderAddress;
    }

    private void Apply(CatchAllSenderAddressRemoved unused)
    {
        _ = unused;
        this._isRemoved = true;
    }

    private void Apply(UserAssignedToCatchAllSender @event)
    {
        this._assignedUserIds.Add(@event.UserId);
    }

    private void Apply(UserUnassignedFromCatchAllSender @event)
    {
        this._assignedUserIds.Remove(@event.UserId);
    }
}
