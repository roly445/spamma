using JasperFx.Events;
using JetBrains.Annotations;
using Marten;
using Marten.Events.Projections;
using Marten.Patching;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Projections;

/// <summary>
/// Projection for push integrations.
/// </summary>
public class PushIntegrationProjection : EventProjection
{
    /// <summary>
    /// Creates a push integration lookup from the created event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>The lookup.</returns>
    [UsedImplicitly]
    public PushIntegrationLookup Create(PushIntegrationCreatedEvent @event)
    {
        return new PushIntegrationLookup
        {
            Id = @event.PushIntegrationId,
            UserId = @event.UserId,
            SubdomainId = @event.SubdomainId,
            Name = @event.Name,
            EndpointUrl = @event.EndpointUrl,
            FilterType = @event.FilterType,
            FilterValue = @event.FilterValue,
            IsActive = true,
            CreatedAt = @event.CreatedAt,
            UpdatedAt = @event.CreatedAt,
        };
    }

    /// <summary>
    /// Projects the updated event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="ops">The document operations.</param>
    [UsedImplicitly]
    public void Project(IEvent<PushIntegrationUpdatedEvent> @event, IDocumentOperations ops)
    {
        ops.Patch<PushIntegrationLookup>(@event.StreamId)
            .Set(x => x.Name, @event.Data.Name)
            .Set(x => x.EndpointUrl, @event.Data.EndpointUrl)
            .Set(x => x.FilterType, @event.Data.FilterType)
            .Set(x => x.FilterValue, @event.Data.FilterValue)
            .Set(x => x.UpdatedAt, @event.Data.UpdatedAt);
    }

    /// <summary>
    /// Projects the deactivated event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="ops">The document operations.</param>
    [UsedImplicitly]
    public void Project(IEvent<PushIntegrationDeactivatedEvent> @event, IDocumentOperations ops)
    {
        ops.Patch<PushIntegrationLookup>(@event.StreamId)
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, @event.Data.DeactivatedAt);
    }

    /// <summary>
    /// Projects the deleted event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="ops">The document operations.</param>
    [UsedImplicitly]
    public void Project(IEvent<PushIntegrationDeletedEvent> @event, IDocumentOperations ops)
    {
        ops.Delete<PushIntegrationLookup>(@event.StreamId);
    }
}