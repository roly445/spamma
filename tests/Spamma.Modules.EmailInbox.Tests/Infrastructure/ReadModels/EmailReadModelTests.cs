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
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            DeletedAt = null,
            CampaignId = null,
            EmailAddresses = new(),
        };

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
        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Favorite Email",
            SentAt = DateTime.UtcNow,
            IsFavorite = true,
            DeletedAt = null,
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
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            DeletedAt = null,
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
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            DeletedAt = deletedTime,
            CampaignId = null,
            EmailAddresses = new(),
        };

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

        var emailLookup = new EmailLookup
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubdomainId = Guid.NewGuid(),
            Subject = "Email with Recipients",
            SentAt = DateTime.UtcNow,
            IsFavorite = false,
            DeletedAt = null,
            CampaignId = null,
            EmailAddresses = emailAddresses,
        };

        // Verify
        emailLookup.EmailAddresses.Should().HaveCount(2);
        emailLookup.EmailAddresses[0].Address.Should().Be("from@example.com");
    }
}