namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class EmailLookup
{
    public Guid Id { get; set; }

    public Guid DomainId { get; set; }

    public Guid SubdomainId { get; set; }

    public List<EmailAddress> EmailAddresses { get; set; } = new();

    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsFavorite { get; set; }

    public Guid? CampaignId { get; set; }

    public string? CampaignValue { get; set; }
}