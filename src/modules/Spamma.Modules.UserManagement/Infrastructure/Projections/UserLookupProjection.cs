using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Projections;

public class UserLookupProjection : EventProjection
{
    [UsedImplicitly]
    public UserLookup Create(UserCreated @event)
    {
        return new UserLookup
        {
            Id = @event.UserId,
            Name = @event.Name,
            EmailAddress = @event.EmailAddress,
            CreatedAt = @event.WhenCreated,
            SystemRole = @event.SystemRole,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<DetailsChanged> @event, IDocumentOperations ops)
    {
        ops.Patch<UserLookup>(@event.StreamId)
            .Set(x => x.EmailAddress, @event.Data.EmailAddress)
            .Set(x => x.Name, @event.Data.Name)
            .Set(x => x.SystemRole, @event.Data.SystemRole);
    }

    [UsedImplicitly]
    public void Project(IEvent<AuthenticationCompleted> @event, IDocumentOperations ops)
    {
        ops.Patch<UserLookup>(@event.StreamId)
            .Set(x => x.LastLoginAt, @event.Data.WhenCompleted);
    }

    [UsedImplicitly]
    public void Project(IEvent<AccountSuspended> @event, IDocumentOperations ops)
    {
        ops.Patch<UserLookup>(@event.StreamId)
            .Set(x => x.IsSuspended, true)
            .Set(x => x.WhenSuspended, @event.Data.WhenSuspended);
    }

    [UsedImplicitly]
    public void Project(IEvent<AccountUnsuspended> @event, IDocumentOperations ops)
    {
        ops.Patch<UserLookup>(@event.StreamId)
            .Set(x => x.IsSuspended, false)
            .Set(x => x.WhenSuspended, null);
    }
}