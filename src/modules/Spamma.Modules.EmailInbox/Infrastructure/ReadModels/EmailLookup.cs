namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class EmailLookup
{
    public Guid Id { get; init; }

    public Guid DomainId { get; init; }

    public Guid SubdomainId { get; init; }

    public IReadOnlyList<EmailAddress> EmailAddresses { get; init; } = [];

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset SentAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public bool IsFavorite { get; init; }

    public Guid? CampaignId { get; init; }

    public string? CampaignValue { get; init; }
}