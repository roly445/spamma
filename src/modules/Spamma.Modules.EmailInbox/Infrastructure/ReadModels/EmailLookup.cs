namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class EmailLookup
{
    private List<EmailAddress> _emailAddresses = new();

    public Guid Id { get; init; }

    public Guid DomainId { get; init; }

    public Guid SubdomainId { get; init; }

    /// <summary>
    /// Gets the email addresses as a read-only collection. Uses init setter to allow population during object initialization.
    /// </summary>
    public IReadOnlyCollection<EmailAddress> EmailAddresses
    {
        get => this._emailAddresses.AsReadOnly();
        init => this._emailAddresses = value?.ToList() ?? new();
    }

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset SentAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public bool IsFavorite { get; init; }

    public Guid? CampaignId { get; init; }

    public string? CampaignValue { get; init; }
}