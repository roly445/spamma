using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

public static class EmailLookupTestFactory
{
    public static EmailLookup Create(
        Guid id,
        Guid subdomainId,
        Guid domainId,
        string subject,
        DateTime sentAt,
        bool isFavorite,
        List<EmailAddress> emailAddresses,
        DateTime? deletedAt = null,
        Guid? campaignId = null,
        string? campaignValue = null)
    {
        return new EmailLookup
        {
            Id = id,
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = subject,
            SentAt = new DateTimeOffset(sentAt),
            IsFavorite = isFavorite,
            DeletedAt = deletedAt,
            CampaignId = campaignId,
            CampaignValue = campaignValue,
            EmailAddresses = emailAddresses,
        };
    }
}
