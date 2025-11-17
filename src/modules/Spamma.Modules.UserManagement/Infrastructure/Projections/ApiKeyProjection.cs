using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Projections;

public class ApiKeyProjection : EventProjection
{
    [UsedImplicitly]
    public ApiKeyLookup Create(ApiKeyCreated @event)
    {
        return new ApiKeyLookup();
    }

    [UsedImplicitly]
    public void Project(IEvent<ApiKeyCreated> @event, IDocumentOperations ops)
    {
        ops.Patch<ApiKeyLookup>(@event.StreamId)
            .Set(x => x.Id, @event.StreamId)
            .Set(x => x.UserId, @event.Data.UserId)
            .Set(x => x.Name, @event.Data.Name)
            .Set(x => x.KeyHashPrefix, @event.Data.KeyHashPrefix)
            .Set(x => x.KeyHash, @event.Data.KeyHash)
            .Set(x => x.CreatedAt, @event.Data.CreatedAt)
            .Set(x => x.ExpiresAt, @event.Data.ExpiresAt);
    }

    [UsedImplicitly]
    public void Project(IEvent<ApiKeyRevoked> @event, IDocumentOperations ops)
    {
        ops.Patch<ApiKeyLookup>(@event.StreamId)
            .Set(x => x.RevokedAt, @event.Data.RevokedAt);
    }
}