using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;

/// <summary>
/// Push integration aggregate for managing email push subscriptions.
/// </summary>
public class PushIntegration : AggregateRoot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PushIntegration"/> class.
    /// </summary>
    /// <param name="events">The events to replay.</param>
    public PushIntegration(IEnumerable<object> events)
    {
        this.LoadFromHistory(events);
    }

    /// <summary>
    /// Gets or sets the ID of the push integration.
    /// </summary>
    public override Guid Id { get; protected set; }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the subdomain ID.
    /// </summary>
    public Guid SubdomainId { get; private set; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Gets the endpoint URL.
    /// </summary>
    public string EndpointUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the filter type.
    /// </summary>
    public FilterType FilterType { get; private set; }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    public string? FilterValue { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the integration is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the created at timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the updated at timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new push integration.
    /// </summary>
    /// <param name="pushIntegrationId">The push integration ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="subdomainId">The subdomain ID.</param>
    /// <param name="name">The name.</param>
    /// <param name="endpointUrl">The endpoint URL.</param>
    /// <param name="filterType">The filter type.</param>
    /// <param name="filterValue">The filter value.</param>
    /// <param name="createdAt">The created at timestamp.</param>
    /// <returns>The push integration.</returns>
    public static PushIntegration Create(
        Guid pushIntegrationId,
        Guid userId,
        Guid subdomainId,
        string? name,
        string endpointUrl,
        FilterType filterType,
        string? filterValue,
        DateTimeOffset createdAt)
    {
        var pushIntegration = new PushIntegration(Enumerable.Empty<object>());
        pushIntegration.RaiseEvent(new PushIntegrationCreatedEvent(
            pushIntegrationId,
            userId,
            subdomainId,
            name,
            endpointUrl,
            filterType,
            filterValue,
            createdAt));
        return pushIntegration;
    }

    /// <summary>
    /// Updates the push integration.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="endpointUrl">The endpoint URL.</param>
    /// <param name="filterType">The filter type.</param>
    /// <param name="filterValue">The filter value.</param>
    /// <param name="updatedAt">The updated at timestamp.</param>
    public void Update(string? name, string endpointUrl, FilterType filterType, string? filterValue, DateTimeOffset updatedAt)
    {
        this.RaiseEvent(new PushIntegrationUpdatedEvent(name, endpointUrl, filterType, filterValue, updatedAt));
    }

    /// <summary>
    /// Deactivates the push integration.
    /// </summary>
    /// <param name="deactivatedAt">The deactivated at timestamp.</param>
    public void Deactivate(DateTimeOffset deactivatedAt)
    {
        this.RaiseEvent(new PushIntegrationDeactivatedEvent(deactivatedAt));
    }

    /// <summary>
    /// Deletes the push integration.
    /// </summary>
    /// <param name="deletedAt">The deleted at timestamp.</param>
    public void Delete(DateTimeOffset deletedAt)
    {
        this.RaiseEvent(new PushIntegrationDeletedEvent(deletedAt));
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case PushIntegrationCreatedEvent created:
                this.Id = created.PushIntegrationId;
                this.UserId = created.UserId;
                this.SubdomainId = created.SubdomainId;
                this.Name = created.Name;
                this.EndpointUrl = created.EndpointUrl;
                this.FilterType = created.FilterType;
                this.FilterValue = created.FilterValue;
                this.IsActive = true;
                this.CreatedAt = created.CreatedAt;
                this.UpdatedAt = created.CreatedAt;
                break;
            case PushIntegrationUpdatedEvent updated:
                this.Name = updated.Name;
                this.EndpointUrl = updated.EndpointUrl;
                this.FilterType = updated.FilterType;
                this.FilterValue = updated.FilterValue;
                this.UpdatedAt = updated.UpdatedAt;
                break;
            case PushIntegrationDeactivatedEvent deactivated:
                this.IsActive = false;
                this.UpdatedAt = deactivated.DeactivatedAt;
                break;
            case PushIntegrationDeletedEvent:
                // Hard delete - no state change needed
                break;
        }
    }
}