using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;

namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

/// <summary>
/// Read model for push integrations.
/// </summary>
public class PushIntegrationLookup
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the subdomain ID.
    /// </summary>
    public Guid SubdomainId { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL.
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter type.
    /// </summary>
    public FilterType FilterType { get; set; }

    /// <summary>
    /// Gets or sets the filter value.
    /// </summary>
    public string? FilterValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the integration is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the created at timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated at timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}