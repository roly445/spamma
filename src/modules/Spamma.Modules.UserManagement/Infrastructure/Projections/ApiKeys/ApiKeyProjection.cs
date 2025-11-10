using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Projections.ApiKeys;

public class ApiKeyProjection : EventProjection
{
    [UsedImplicitly]
    public ApiKeyLookup Create(ApiKeyCreated @event)
    {
        return new ApiKeyLookup
        {
            Id = @event.ApiKeyId,
            UserId = @event.UserId,
            Name = @event.Name,
            KeyHash = @event.KeyHash,
            Salt = @event.Salt,
            CreatedAt = @event.CreatedAt,
        };
    }

    [UsedImplicitly]
    public void Project(IEvent<ApiKeyRevoked> @event, IDocumentOperations ops)
    {
        ops.Patch<ApiKeyLookup>(@event.StreamId)
            .Set(x => x.RevokedAt, @event.Data.RevokedAt);
    }
}