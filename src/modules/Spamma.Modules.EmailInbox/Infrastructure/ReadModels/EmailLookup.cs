namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class EmailLookup
{
    public Guid Id { get; init; }

    public Guid DomainId { get; init; }

    public Guid SubdomainId { get; init; }

    public IReadOnlyList<EmailAddress> EmailAddresses { get; init; } = [];

    public string Subject { get; init; } = string.Empty;

    // Null marks an older message that still needs its stored EML backfilled.
    public string? SearchableBody { get; init; }

    public string? BodyIndexError { get; init; }

    // Null marks messages whose stored MIME has not yet been inspected for attachments.
    public bool? HasAttachment { get; init; }

    public string? AttachmentIndexError { get; init; }

    public DateTimeOffset SentAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public bool IsFavorite { get; init; }

    public Guid? CampaignId { get; init; }

    public string? CampaignValue { get; init; }

    public Guid? CatchAllSenderAddressId { get; init; }
}
