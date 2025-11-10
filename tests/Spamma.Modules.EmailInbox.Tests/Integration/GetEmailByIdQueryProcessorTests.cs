using FluentAssertions;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class GetEmailByIdQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidEmailId_ReturnsEmailDetails()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var expectedWhenSent = DateTime.UtcNow.AddDays(-5);

        var email = new EmailLookup
        {
            Id = emailId,
            SubdomainId = subdomainId,
            Subject = "Test Email Subject",
            WhenSent = expectedWhenSent,
            IsFavorite = true,
            CampaignId = campaignId,
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("to@example.com", "To Name", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var query = new GetEmailByIdQuery(emailId);

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(emailId);
        result.Data.SubdomainId.Should().Be(subdomainId);
        result.Data.Subject.Should().Be("Test Email Subject");
        result.Data.IsFavorite.Should().BeTrue();
        result.Data.CampaignId.Should().Be(campaignId);
        result.Data.WhenSent.Should().BeCloseTo(expectedWhenSent, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_WithNonExistentEmailId_ReturnsFailed()
    {
        // Arrange
        var nonExistentEmailId = Guid.NewGuid();
        var query = new GetEmailByIdQuery(nonExistentEmailId);

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        var action = () => result.Data;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithDeletedEmail_StillReturnsEmail()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = emailId,
            SubdomainId = subdomainId,
            Subject = "Deleted Email",
            WhenSent = DateTime.UtcNow.AddDays(-3),
            WhenDeleted = DateTime.UtcNow.AddHours(-1),
            IsFavorite = false,
            CampaignId = null,
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("deleted@example.com", "Deleted", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var query = new GetEmailByIdQuery(emailId);

        // Act
        var result = await this.Sender.Send(query);

        // Assert - GetEmailByIdQuery doesn't filter deleted emails
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(emailId);
        result.Data.Subject.Should().Be("Deleted Email");
    }

    [Fact]
    public async Task Handle_WithEmailWithoutCampaign_ReturnsCampaignIdNull()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = emailId,
            SubdomainId = subdomainId,
            Subject = "Email without campaign",
            WhenSent = DateTime.UtcNow.AddDays(-2),
            IsFavorite = false,
            CampaignId = null,
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress("nocampaign@example.com", "No Campaign", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var query = new GetEmailByIdQuery(emailId);

        // Act
        var result = await this.Sender.Send(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(emailId);
        result.Data.CampaignId.Should().BeNull();
        result.Data.IsFavorite.Should().BeFalse();
    }
}
