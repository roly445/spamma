namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class EmailLookup
{
    public Guid Id { get; internal set; }

    public Guid DomainId { get; internal set; }

    public Guid SubdomainId { get; internal set; }

    public List<EmailAddress> EmailAddresses { get; internal set; } = new();

    public string Subject { get; internal set; } = string.Empty;

    public DateTimeOffset SentAt { get; internal set; }

    public DateTime? DeletedAt { get; internal set; }

    public bool IsFavorite { get; internal set; }

    public Guid? CampaignId { get; internal set; }

    public string? CampaignValue { get; private set; }
}