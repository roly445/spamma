using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Projections;

public class PasskeyProjection : EventProjection
{
    [UsedImplicitly]
    public PasskeyLookup Create(PasskeyRegistered @event, IEvent eventMeta)
    {
        return new PasskeyLookup();
    }

    [UsedImplicitly]
    public void Project(IEvent<PasskeyRegistered> @event, IDocumentOperations ops)
    {
        ops.Patch<PasskeyLookup>(@event.StreamId)
            .Set(x => x.Id, @event.StreamId)
            .Set(x => x.UserId, @event.Data.UserId)
            .Set(x => x.CredentialId, @event.Data.CredentialId)
            .Set(x => x.DisplayName, @event.Data.DisplayName)
            .Set(x => x.Algorithm, @event.Data.Algorithm)
            .Set(x => x.RegisteredAt, @event.Data.RegisteredAt)
            .Set(x => x.LastUsedAt, null)
            .Set(x => x.IsRevoked, false)
            .Set(x => x.RevokedAt, null);
    }

    [UsedImplicitly]
    public void Project(IEvent<PasskeyAuthenticated> @event, IDocumentOperations ops)
    {
        ops.Patch<PasskeyLookup>(@event.StreamId)
            .Set(x => x.LastUsedAt, @event.Data.UsedAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<PasskeyRevoked> @event, IDocumentOperations ops)
    {
        ops.Patch<PasskeyLookup>(@event.StreamId)
            .Set(x => x.IsRevoked, true)
            .Set(x => x.RevokedAt, @event.Data.RevokedAt);
    }
}