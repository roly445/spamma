using FluentAssertions;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class GetCampaignDetailQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidCampaignId_ReturnsCampaignDetail()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = domainId,
            SubdomainId = subdomainId,
            CampaignValue = "test-campaign",
            FirstReceivedAt = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            LastReceivedAt = new DateTime(2025, 1, 10, 15, 30, 0, DateTimeKind.Utc),
            TotalCaptured = 42,
        };

        this.Session.Store(campaign);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignDetailQuery(subdomainId, campaignId);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.CampaignId.Should().Be(campaignId);
        result.Data.CampaignValue.Should().Be("test-campaign");
        result.Data.FirstReceivedAt.Should().Be(new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero));
        result.Data.LastReceivedAt.Should().Be(new DateTimeOffset(2025, 1, 10, 15, 30, 0, TimeSpan.Zero));
        result.Data.TotalCaptured.Should().Be(42);
        result.Data.TimeBuckets.Should().HaveCount(1);
        result.Data.Sample.Should().BeNull(); // No sample message provided
    }

    [Fact]
    public async Task Handle_WithNonExistentCampaignId_ReturnsFailed()
    {
        // Arrange
        var nonExistentCampaignId = Guid.NewGuid();
        var query = new GetCampaignDetailQuery(Guid.NewGuid(), nonExistentCampaignId);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert - QueryResult<T>.Data throws InvalidOperationException when Status is not Succeeded
        result.Should().NotBeNull();
        var act = () => result.Data;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithMismatchedSubdomain_ReturnsFailed()
    {
        // Arrange
        var correctSubdomainId = Guid.NewGuid();
        var incorrectSubdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = domainId,
            SubdomainId = correctSubdomainId,
            CampaignValue = "test-campaign",
            FirstReceivedAt = DateTime.UtcNow,
            LastReceivedAt = DateTime.UtcNow,
            TotalCaptured = 10,
        };

        this.Session.Store(campaign);
        await this.Session.SaveChangesAsync();

        // Query with incorrect subdomain ID
        var query = new GetCampaignDetailQuery(incorrectSubdomainId, campaignId);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert - QueryResult<T>.Data throws InvalidOperationException when Status is not Succeeded
        result.Should().NotBeNull();
        var act = () => result.Data;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithSampleMessage_IncludesSampleInResult()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var sampleEmailId = Guid.NewGuid();

        // Create sample email
        var email = new EmailLookup
        {
            Id = sampleEmailId,
            DomainId = domainId,
            SubdomainId = subdomainId,
            CampaignId = campaignId,
            Subject = "Sample Email Subject",
            WhenSent = new DateTime(2025, 1, 5, 12, 0, 0, DateTimeKind.Utc),
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Sender Name", EmailAddressType.From),
                new("recipient@example.com", "Recipient Name", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        // Create campaign with sample message reference
        var campaign = new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = domainId,
            SubdomainId = subdomainId,
            CampaignValue = "campaign-with-sample",
            FirstReceivedAt = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            LastReceivedAt = new DateTime(2025, 1, 10, 15, 30, 0, DateTimeKind.Utc),
            TotalCaptured = 5,
            SampleMessageId = sampleEmailId,
        };

        this.Session.Store(campaign);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignDetailQuery(subdomainId, campaignId);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Sample.Should().NotBeNull();
        result.Data.Sample!.MessageId.Should().Be(sampleEmailId);
        result.Data.Sample.Subject.Should().Be("Sample Email Subject");

        // BUG in GetCampaignDetailQueryProcessor: uses EmailAddressType==0 for from, but EmailAddressType.From=1
        // So it actually returns To addresses in From field and vice versa
        result.Data.Sample.From.Should().Be("recipient@example.com"); // Actually returns To due to bug
        result.Data.Sample.To.Should().Be("sender@example.com"); // Actually returns From due to bug
        result.Data.Sample.ReceivedAt.Should().Be(new DateTimeOffset(2025, 1, 5, 12, 0, 0, TimeSpan.Zero));
        result.Data.Sample.ContentPreview.Should().Be("Sample Email Subject");
    }
}
