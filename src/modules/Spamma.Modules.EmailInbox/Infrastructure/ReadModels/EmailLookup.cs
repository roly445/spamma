namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class EmailLookup
{
    public Guid Id { get; set; }

    public Guid DomainId { get; set; }

    public Guid SubdomainId { get; set; }

    public List<EmailAddress> EmailAddresses { get; set; } = new();

    public string Subject { get; set; } = string.Empty;

    public DateTime WhenSent { get; set; }

    public DateTime? WhenDeleted { get; set; }

    public bool IsFavorite { get; set; }
}