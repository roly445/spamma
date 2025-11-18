using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class GetEmailByIdQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidEmailId_ReturnsEmailDetails()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var expectedWhenSent = DateTime.UtcNow.AddDays(-5);

        var email = EmailLookupTestFactory.Create(
            id: emailId,
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Test Email Subject",
            sentAt: expectedWhenSent,
            isFavorite: true,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("recipient@example.com", "Recipient", EmailAddressType.To),
            },
            deletedAt: null,
            campaignId: campaignId);

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
        var domainId = Guid.NewGuid();

        var email = EmailLookupTestFactory.Create(
            id: emailId,
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "Deleted Email",
            sentAt: DateTime.UtcNow.AddDays(-3),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("deleted@example.com", "Deleted", EmailAddressType.To),
            },
            deletedAt: DateTime.UtcNow.AddHours(-1),
            campaignId: null);

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
        var domainId = Guid.NewGuid();

        var email = EmailLookupTestFactory.Create(
            id: emailId,
            subdomainId: subdomainId,
            domainId: domainId,
            subject: "No Campaign",
            sentAt: DateTime.UtcNow.AddDays(-2),
            isFavorite: false,
            emailAddresses: new List<EmailAddress>
            {
                new EmailAddress("nocampaign@example.com", "No Campaign", EmailAddressType.To),
            },
            deletedAt: null,
            campaignId: null);

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