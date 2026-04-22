using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

public class CatchAllSenderAddressLookupProjection : EventProjection
{
    [UsedImplicitly]
    public CatchAllSenderAddressLookup Create(CatchAllSenderAddressAdded @event)
    {
        return new CatchAllSenderAddressLookup
        {
            Id = @event.AddressId,
            SenderAddress = @event.SenderAddress,
            AddedAt = @event.AddedAt,
            IsRemoved = false,
            AssignedUserIds = [],
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<CatchAllSenderAddressRemoved> @event, IDocumentOperations ops)
    {
        ops.Patch<CatchAllSenderAddressLookup>(@event.StreamId)
            .Set(x => x.IsRemoved, true);
    }

    [UsedImplicitly]
    public void Project(IEvent<UserAssignedToCatchAllSender> @event, IDocumentOperations ops)
    {
        ops.Patch<CatchAllSenderAddressLookup>(@event.StreamId)
            .Append(x => x.AssignedUserIds, @event.Data.UserId);
    }

    [UsedImplicitly]
    public void Project(IEvent<UserUnassignedFromCatchAllSender> @event, IDocumentOperations ops)
    {
        ops.Patch<CatchAllSenderAddressLookup>(@event.StreamId)
            .Remove(x => x.AssignedUserIds, @event.Data.UserId);
    }
}
