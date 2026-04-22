using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

public partial class CatchAllSenderAddress : AggregateRoot
{
    private readonly List<Guid> _assignedUserIds = new();
    private string _senderAddress = string.Empty;
    private bool _isRemoved;

    private CatchAllSenderAddress()
    {
    }

    public override Guid Id { get; protected set; }

    internal string SenderAddress => this._senderAddress;

    internal IReadOnlyList<Guid> AssignedUserIds => this._assignedUserIds;

    internal bool IsRemoved => this._isRemoved;

    internal static Result<CatchAllSenderAddress, BluQubeErrorData> Create(Guid addressId, string senderAddress, DateTimeOffset addedAt)
    {
        var address = new CatchAllSenderAddress();
        var @event = new CatchAllSenderAddressAdded(addressId, senderAddress, addedAt);
        address.RaiseEvent(@event);

        return Result.Ok<CatchAllSenderAddress, BluQubeErrorData>(address);
    }

    internal ResultWithError<BluQubeErrorData> Remove(DateTimeOffset removedAt)
    {
        if (this._isRemoved)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.CatchAllSenderAddressAlreadyRemoved,
                $"Catch-all sender address '{this.Id}' has already been removed."));
        }

        var @event = new CatchAllSenderAddressRemoved(removedAt);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> AssignUser(Guid userId)
    {
        if (this._isRemoved)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.CatchAllSenderAddressRemoved,
                $"Cannot assign user to removed catch-all sender address '{this.Id}'."));
        }

        if (this._assignedUserIds.Contains(userId))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.UserAlreadyAssignedToCatchAllSender,
                $"User '{userId}' is already assigned to catch-all sender address '{this.Id}'."));
        }

        var @event = new UserAssignedToCatchAllSender(userId);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> UnassignUser(Guid userId)
    {
        if (this._isRemoved)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.CatchAllSenderAddressRemoved,
                $"Cannot unassign user from removed catch-all sender address '{this.Id}'."));
        }

        if (!this._assignedUserIds.Contains(userId))
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.UserNotAssignedToCatchAllSender,
                $"User '{userId}' is not assigned to catch-all sender address '{this.Id}'."));
        }

        var @event = new UserUnassignedFromCatchAllSender(userId);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }
}
