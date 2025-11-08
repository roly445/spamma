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
        return new PasskeyLookup
        {
            Id = eventMeta.StreamId,
            UserId = @event.UserId,
            CredentialId = @event.CredentialId,
            DisplayName = @event.DisplayName,
            Algorithm = @event.Algorithm,
            RegisteredAt = @event.RegisteredAt,
            LastUsedAt = null,
            IsRevoked = false,
            RevokedAt = null,
        };
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