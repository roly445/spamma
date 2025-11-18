using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.ReadModels;

public class EmailReadModelTests
{
    [Fact]
    public void EmailLookup_Creation_WithValidData()
    {
        // Arrange & Act
        var emailLookup = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            domainId: Guid.NewGuid(),
            subdomainId: Guid.NewGuid(),
            subject: "Test Subject",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>(),
            deletedAt: null,
            campaignId: null);

        // Verify
        emailLookup.Should().NotBeNull();
        emailLookup.Id.Should().NotBe(Guid.Empty);
        emailLookup.Subject.Should().Be("Test Subject");
        emailLookup.IsFavorite.Should().BeFalse();
        emailLookup.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void EmailLookup_Favorited()
    {
        // Arrange & Act
        var emailLookup = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            domainId: Guid.NewGuid(),
            subdomainId: Guid.NewGuid(),
            subject: "Favorite Email",
            sentAt: DateTime.UtcNow,
            isFavorite: true,
            emailAddresses: new List<EmailAddress>(),
            deletedAt: null,
            campaignId: null);

        // Verify
        emailLookup.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void EmailLookup_AssignedToCampaign()
    {
        // Arrange & Act
        var campaignId = Guid.NewGuid();
        var emailLookup = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            domainId: Guid.NewGuid(),
            subdomainId: Guid.NewGuid(),
            subject: "Campaign Email",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>(),
            deletedAt: null,
            campaignId: campaignId);

        // Verify
        emailLookup.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public void EmailLookup_Deleted()
    {
        // Arrange
        var deletedTime = DateTime.UtcNow;

        // Act
        var emailLookup = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            domainId: Guid.NewGuid(),
            subdomainId: Guid.NewGuid(),
            subject: "Deleted Email",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: new List<EmailAddress>(),
            deletedAt: deletedTime,
            campaignId: null);

        // Verify
        emailLookup.DeletedAt.Should().Be(deletedTime);
    }

    [Fact]
    public void EmailLookup_WithEmailAddresses()
    {
        // Arrange & Act
        var emailAddresses = new List<EmailAddress>
        {
            new("from@example.com", "Sender", EmailAddressType.From),
            new("to@example.com", "Recipient", EmailAddressType.To),
        };

        var emailLookup = EmailLookupTestFactory.Create(
            id: Guid.NewGuid(),
            domainId: Guid.NewGuid(),
            subdomainId: Guid.NewGuid(),
            subject: "Email with Recipients",
            sentAt: DateTime.UtcNow,
            isFavorite: false,
            emailAddresses: emailAddresses,
            deletedAt: null,
            campaignId: null);

        // Verify
        emailLookup.EmailAddresses.Should().HaveCount(2);
        emailLookup.EmailAddresses[0].Address.Should().Be("from@example.com");
    }
}
