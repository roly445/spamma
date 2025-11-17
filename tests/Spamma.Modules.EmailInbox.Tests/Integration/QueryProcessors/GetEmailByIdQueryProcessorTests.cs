using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration.QueryProcessors;

public class GetEmailByIdQueryProcessorTests : QueryProcessorIntegrationTestBase
{
    public GetEmailByIdQueryProcessorTests()
        : base()
    {
    }

    [Fact]
    public async Task Handle_WithValidEmailId_ReturnsEmail()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Test Email",
            SentAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            IsFavorite = true,
            CampaignId = campaignId,
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var query = new GetEmailByIdQuery(email.Id);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(email.Id);
        result.Data.SubdomainId.Should().Be(subdomainId);
        result.Data.Subject.Should().Be("Test Email");
        result.Data.WhenSent.Should().Be(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        result.Data.IsFavorite.Should().BeTrue();
        result.Data.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmailId_ReturnsFailed()
    {
        // Arrange
        var nonExistentEmailId = Guid.NewGuid();
        var query = new GetEmailByIdQuery(nonExistentEmailId);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert - QueryResult<T>.Data throws InvalidOperationException when Status is not Succeeded
        result.Should().NotBeNull();
        var act = () => result.Data;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithEmailWithoutCampaign_ReturnsEmailWithNullCampaignId()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Email without campaign",
            SentAt = new DateTime(2025, 1, 2, 14, 0, 0, DateTimeKind.Utc),
            IsFavorite = false,
            CampaignId = null, // No campaign association
            EmailAddresses = new List<EmailAddress>
            {
                new("sender@example.com", "Sender", EmailAddressType.From),
                new("recipient@example.com", "Recipient", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var query = new GetEmailByIdQuery(email.Id);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(email.Id);
        result.Data.CampaignId.Should().BeNull();
        result.Data.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithFavoriteEmail_ReturnsFavoriteTrue()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DomainId = domainId,
            Subject = "Favorite Email",
            SentAt = DateTime.UtcNow,
            IsFavorite = true,
            CampaignId = null,
            EmailAddresses = new List<EmailAddress>
            {
                new("important@example.com", "Important Person", EmailAddressType.From),
                new("me@example.com", "Me", EmailAddressType.To),
            },
        };

        this.Session.Store(email);
        await this.Session.SaveChangesAsync();

        var query = new GetEmailByIdQuery(email.Id);

        // Act
        var result = await this.Sender.Send(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.IsFavorite.Should().BeTrue();
        result.Data.Subject.Should().Be("Favorite Email");
    }
}