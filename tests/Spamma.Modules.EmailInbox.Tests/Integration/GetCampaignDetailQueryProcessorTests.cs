using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class GetCampaignDetailQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidCampaignId_ReturnsCampaignDetails()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var emailId = Guid.NewGuid();

        // Create email for sample message
        var email = EmailLookupTestFactory.Create(
            id: emailId,
            subdomainId: subdomainId,
            domainId: Guid.NewGuid(),
            subject: "Sample Email Subject",
            sentAt: DateTime.UtcNow.AddDays(-5),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("sender@example.com", "Sender Name", EmailAddressType.From),
                new EmailAddress("recipient@example.com", "Recipient Name", EmailAddressType.To),
            });

        this.Session.Store(email);
        this.PersistEmailAddresses(email);

        // Create campaign with sample message
        var campaign = new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = Guid.NewGuid(),
            SubdomainId = subdomainId,
            CampaignValue = "test-campaign",
            FirstReceivedAt = DateTime.UtcNow.AddDays(-10),
            LastReceivedAt = DateTime.UtcNow,
            TotalCaptured = 25,
            SampleMessageId = emailId,
        };

        this.Session.Store(campaign);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignDetailQuery(SubdomainId: subdomainId, CampaignId: campaignId);

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.CampaignId.Should().Be(campaignId);
        result.Data.CampaignValue.Should().Be("test-campaign");
        result.Data.TotalCaptured.Should().Be(25);
        result.Data.Sample.Should().NotBeNull();
        result.Data.Sample!.MessageId.Should().Be(emailId);
        result.Data.Sample.Subject.Should().Be("Sample Email Subject");

        // With corrected processor logic: From contains sender, To contains recipient(s)
        result.Data.Sample.From.Should().Be("sender@example.com");
        result.Data.Sample.To.Should().Be("recipient@example.com");
    }

    [Fact]
    public async Task Handle_WithNonExistentCampaignId_ReturnsFailed()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var nonExistentCampaignId = Guid.NewGuid();
        var query = new GetCampaignDetailQuery(SubdomainId: subdomainId, CampaignId: nonExistentCampaignId);

        // Act
        var result = await this.Querier.Send(query);

        // Assert
        var action = () => result.Data;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithMismatchedSubdomainId_ReturnsFailed()
    {
        // Arrange
        var actualSubdomainId = Guid.NewGuid();
        var differentSubdomainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = Guid.NewGuid(),
            SubdomainId = actualSubdomainId,
            CampaignValue = "test-campaign",
            FirstReceivedAt = DateTime.UtcNow.AddDays(-10),
            LastReceivedAt = DateTime.UtcNow,
            TotalCaptured = 10,
        };

        this.Session.Store(campaign);
        await this.Session.SaveChangesAsync();

        var query = new GetCampaignDetailQuery(SubdomainId: differentSubdomainId, CampaignId: campaignId);

        // Act
        var result = await this.Querier.Send(query);

        // Assert
        var action = () => result.Data;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithEmptySubdomainId_ReturnsAllCampaignData()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = Guid.NewGuid(),
            SubdomainId = subdomainId,
            CampaignValue = "test-campaign",
            FirstReceivedAt = DateTime.UtcNow.AddDays(-10),
            LastReceivedAt = DateTime.UtcNow,
            TotalCaptured = 15,
        };

        this.Session.Store(campaign);
        await this.Session.SaveChangesAsync();

        // Query with Guid.Empty for SubdomainId (no subdomain filter)
        var query = new GetCampaignDetailQuery(SubdomainId: Guid.Empty, CampaignId: campaignId);

        // Act
        var result = await this.Querier.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.CampaignId.Should().Be(campaignId);
        result.Data.TotalCaptured.Should().Be(15);
    }
}
