using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

public partial class CatchAllSenderAddress
{
    public static CatchAllSenderAddress Create(CatchAllSenderAddressAdded @event)
    {
        var address = new CatchAllSenderAddress();
        address.Apply(@event);
        return address;
    }

    public void Apply(CatchAllSenderAddressAdded @event)
    {
        this.Id = @event.AddressId;
        this._senderAddress = @event.SenderAddress;
    }

    public void Apply(CatchAllSenderAddressRemoved unused)
    {
        _ = unused;
        this._isRemoved = true;
    }

    public void Apply(UserAssignedToCatchAllSender @event)
    {
        this._assignedUserIds.Add(@event.UserId);
    }

    public void Apply(UserUnassignedFromCatchAllSender @event)
    {
        this._assignedUserIds.Remove(@event.UserId);
    }

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
}
