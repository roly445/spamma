using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Projections;

/// <summary>
/// Marten projection that creates and maintains PasskeySummaryReadModel documents.
/// Optimizes passkey queries by denormalizing event data into a searchable read model.
/// </summary>
public class PasskeyProjection : EventProjection
{
    /// <summary>
    /// Creates a new PasskeySummaryReadModel when a passkey is registered.
    /// </summary>
    /// <param name="event">The PasskeyRegistered domain event.</param>
    /// <param name="eventMeta">Marten event metadata including the stream ID.</param>
    /// <returns>A new PasskeySummaryReadModel initialized with event data.</returns>
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

    /// <summary>
    /// Updates the LastUsedAt timestamp when a passkey is used for authentication.
    /// </summary>
    /// <param name="event">The PasskeyAuthenticated domain event with timestamp.</param>
    /// <param name="ops">The document operations for Marten patch updates.</param>
    [UsedImplicitly]
    public void Project(IEvent<PasskeyAuthenticated> @event, IDocumentOperations ops)
    {
        ops.Patch<PasskeyLookup>(@event.StreamId)
            .Set(x => x.LastUsedAt, @event.Data.UsedAt);
    }

    /// <summary>
    /// Marks the passkey as revoked when a revocation event is recorded.
    /// </summary>
    /// <param name="event">The PasskeyRevoked domain event with revocation details.</param>
    /// <param name="ops">The document operations for Marten patch updates.</param>
    [UsedImplicitly]
    public void Project(IEvent<PasskeyRevoked> @event, IDocumentOperations ops)
    {
        ops.Patch<PasskeyLookup>(@event.StreamId)
            .Set(x => x.IsRevoked, true)
            .Set(x => x.RevokedAt, @event.Data.RevokedAt);
    }
}