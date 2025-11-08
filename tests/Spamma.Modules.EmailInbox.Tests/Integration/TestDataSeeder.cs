using Marten;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

/// <summary>
/// Helper for seeding test data into Marten during integration tests.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Create and store a campaign summary for testing.
    /// </summary>
    public static async Task<CampaignSummary> CreateCampaignAsync(
        IDocumentSession session,
        Guid? campaignId = null,
        Guid? domainId = null,
        Guid? subdomainId = null,
        string? campaignValue = null,
        int totalCaptured = 5,
        CancellationToken cancellationToken = default)
    {
        var campaign = new CampaignSummary
        {
            CampaignId = campaignId ?? Guid.NewGuid(),
            DomainId = domainId ?? Guid.NewGuid(),
            SubdomainId = subdomainId ?? Guid.NewGuid(),
            CampaignValue = campaignValue ?? $"campaign-{Guid.NewGuid():N}".Substring(0, 50),
            FirstReceivedAt = DateTime.UtcNow.AddDays(-10),
            LastReceivedAt = DateTime.UtcNow,
            TotalCaptured = totalCaptured,
        };

        session.Store(campaign);
        await session.SaveChangesAsync(cancellationToken);

        return campaign;
    }

    /// <summary>
    /// Create and store multiple campaigns for testing.
    /// </summary>
    public static async Task<List<CampaignSummary>> CreateCampaignsAsync(
        IDocumentSession session,
        Guid subdomainId,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        var campaigns = new List<CampaignSummary>();
        var baseTime = DateTime.UtcNow.AddDays(-10);

        for (int i = 0; i < count; i++)
        {
            // Create campaigns with distinct timestamps for sorting tests
            var campaign = new CampaignSummary
            {
                CampaignId = Guid.NewGuid(),
                DomainId = Guid.NewGuid(),
                SubdomainId = subdomainId,
                CampaignValue = $"campaign-{i:D3}",
                FirstReceivedAt = baseTime.AddHours(i),
                LastReceivedAt = baseTime.AddDays(10).AddHours(i),  // Distinct timestamps for each campaign
                TotalCaptured = (i + 1) * 3,
            };

            session.Store(campaign);
            campaigns.Add(campaign);
        }

        await session.SaveChangesAsync(cancellationToken);

        return campaigns;
    }

    /// <summary>
    /// Create and store an email lookup for testing.
    /// </summary>
    public static async Task<EmailLookup> CreateEmailAsync(
        IDocumentSession session,
        Guid? emailId = null,
        Guid? subdomainId = null,
        Guid? domainId = null,
        string? subject = null,
        CancellationToken cancellationToken = default)
    {
        var email = new EmailLookup
        {
            Id = emailId ?? Guid.NewGuid(),
            SubdomainId = subdomainId ?? Guid.NewGuid(),
            DomainId = domainId ?? Guid.NewGuid(),
            Subject = subject ?? $"Test Email {Guid.NewGuid():N}".Substring(0, 50),
            WhenSent = DateTime.UtcNow,
            IsFavorite = false,
            EmailAddresses = new()
            {
                new EmailAddress($"test-{Guid.NewGuid():N}@example.com", "Test User", EmailAddressType.From),
            },
        };

        session.Store(email);
        await session.SaveChangesAsync(cancellationToken);

        return email;
    }

    /// <summary>
    /// Create and store multiple emails for testing.
    /// </summary>
    public static async Task<List<EmailLookup>> CreateEmailsAsync(
        IDocumentSession session,
        Guid subdomainId,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        var emails = new List<EmailLookup>();

        for (int i = 0; i < count; i++)
        {
            var email = await CreateEmailAsync(
                session,
                subdomainId: subdomainId,
                subject: $"Email Subject {i}",
                cancellationToken: cancellationToken);

            emails.Add(email);
        }

        return emails;
    }
}
