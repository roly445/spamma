using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.ReadModels;

public class EmailReadModelTests
{
    [Fact]
    public void EmailLookup_Creation_WithValidData()
    {
        // Arrange & Act
        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Test Subject",
            WhenSent = DateTime.UtcNow,
            IsFavorite = false,
            WhenDeleted = null,
            CampaignId = null,
            EmailAddresses = new(),
        };

        // Verify
        emailLookup.Should().NotBeNull();
        emailLookup.Id.Should().NotBe(Guid.Empty);
        emailLookup.Subject.Should().Be("Test Subject");
        emailLookup.IsFavorite.Should().BeFalse();
        emailLookup.WhenDeleted.Should().BeNull();
    }

    [Fact]
    public void EmailLookup_Favorited()
    {
        // Arrange & Act
        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Favorite Email",
            WhenSent = DateTime.UtcNow,
            IsFavorite = true,
            WhenDeleted = null,
            CampaignId = null,
            EmailAddresses = new(),
        };

        // Verify
        emailLookup.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void EmailLookup_AssignedToCampaign()
    {
        // Arrange & Act
        var campaignId = Guid.NewGuid();
        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Campaign Email",
            WhenSent = DateTime.UtcNow,
            IsFavorite = false,
            WhenDeleted = null,
            CampaignId = campaignId,
            EmailAddresses = new(),
        };

        // Verify
        emailLookup.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public void EmailLookup_Deleted()
    {
        // Arrange
        var deletedTime = DateTime.UtcNow;

        // Act
        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Deleted Email",
            WhenSent = DateTime.UtcNow,
            IsFavorite = false,
            WhenDeleted = deletedTime,
            CampaignId = null,
            EmailAddresses = new(),
        };

        // Verify
        emailLookup.WhenDeleted.Should().Be(deletedTime);
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

        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Email with Recipients",
            WhenSent = DateTime.UtcNow,
            IsFavorite = false,
            WhenDeleted = null,
            CampaignId = null,
            EmailAddresses = emailAddresses,
        };

        // Verify
        emailLookup.EmailAddresses.Should().HaveCount(2);
        emailLookup.EmailAddresses[0].Address.Should().Be("from@example.com");
    }
}
