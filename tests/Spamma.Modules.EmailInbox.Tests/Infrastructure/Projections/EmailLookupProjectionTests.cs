using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.Projections;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Projections;

public class EmailLookupProjectionTests
{
    [Fact]
    public void Create_MapsEmailReceived_ToReadModel()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var whenSent = DateTime.UtcNow;
        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("sender@example.com", "Sender Name", EmailAddressType.From),
            new("recipient@example.com", "Recipient Name", EmailAddressType.To),
        };

        var evt = new EmailReceived(emailId, domainId, subdomainId, "Test Subject", whenSent, emailAddresses);

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.Id.Should().Be(emailId);
        readModel.DomainId.Should().Be(domainId);
        readModel.SubdomainId.Should().Be(subdomainId);
        readModel.Subject.Should().Be("Test Subject");
        readModel.WhenSent.Should().Be(whenSent);
        readModel.IsFavorite.Should().BeFalse();
        readModel.WhenDeleted.Should().BeNull();
        readModel.CampaignId.Should().BeNull();
        readModel.EmailAddresses.Should().HaveCount(2);
    }

    [Fact]
    public void Create_InitializesCampaignIdAsNull()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var evt = new EmailReceived(
            emailId, Guid.NewGuid(), Guid.NewGuid(), "Subject", DateTime.UtcNow, new List<EmailReceived.EmailAddress>());

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.CampaignId.Should().BeNull();
    }

    [Fact]
    public void Create_PreservesEmailAddressDetails()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("from@example.com", "From Name", EmailAddressType.From),
            new("to@example.com", "To Name", EmailAddressType.To),
            new("cc@example.com", "Cc Name", EmailAddressType.Cc),
        };

        var evt = new EmailReceived(
            emailId, Guid.NewGuid(), Guid.NewGuid(), "Subject", DateTime.UtcNow, emailAddresses);

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.EmailAddresses.Should().HaveCount(3);
        readModel.EmailAddresses[0].Address.Should().Be("from@example.com");
        readModel.EmailAddresses[0].Name.Should().Be("From Name");
        readModel.EmailAddresses[1].Address.Should().Be("to@example.com");
        readModel.EmailAddresses[2].Address.Should().Be("cc@example.com");
    }

    [Fact]
    public void Create_SetsAllPropertiesFromEvent()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var whenSent = new DateTime(2024, 10, 15, 10, 30, 0, DateTimeKind.Utc);
        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To),
        };

        var evt = new EmailReceived(emailId, domainId, subdomainId, "Subject Line", whenSent, emailAddresses);

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.Id.Should().Be(emailId);
        readModel.DomainId.Should().Be(domainId);
        readModel.SubdomainId.Should().Be(subdomainId);
        readModel.Subject.Should().Be("Subject Line");
        readModel.WhenSent.Should().Be(whenSent);
        readModel.EmailAddresses.Should().ContainSingle(x => x.Address == "test@example.com");
    }

    [Fact]
    public void Create_HandlesEmptyEmailAddressList()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var evt = new EmailReceived(
            emailId, Guid.NewGuid(), Guid.NewGuid(), "Subject", DateTime.UtcNow, new List<EmailReceived.EmailAddress>());

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.EmailAddresses.Should().BeEmpty();
        readModel.IsFavorite.Should().BeFalse();
        readModel.WhenDeleted.Should().BeNull();
    }

    [Fact]
    public void Create_PreservesSubdomainIdForEmailRouting()
    {
        // Arrange - Verify subdomain ID is preserved for domain-based routing
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var evt = new EmailReceived(
            emailId, domainId, subdomainId, "Test", DateTime.UtcNow, new List<EmailReceived.EmailAddress>());

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.SubdomainId.Should().Be(subdomainId);
        readModel.DomainId.Should().Be(domainId);
    }

    [Fact]
    public void Create_MapsMultipleEmailAddressTypes()
    {
        // Arrange - Test with multiple address types (From, To, Cc, Bcc)
        var emailId = Guid.NewGuid();
        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("from@domain.com", "Sender", EmailAddressType.From),
            new("to1@domain.com", "Recipient1", EmailAddressType.To),
            new("to2@domain.com", "Recipient2", EmailAddressType.To),
            new("cc@domain.com", "CC Recipient", EmailAddressType.Cc),
            new("bcc@domain.com", "BCC Recipient", EmailAddressType.Bcc),
        };

        var evt = new EmailReceived(emailId, Guid.NewGuid(), Guid.NewGuid(), "Subject", DateTime.UtcNow, emailAddresses);

        var projection = new EmailLookupProjection();

        // Act
        var readModel = projection.Create(evt);

        // Assert
        readModel.EmailAddresses.Should().HaveCount(5);
        readModel.EmailAddresses.Should().ContainSingle(x => x.EmailAddressType == EmailAddressType.From);
        readModel.EmailAddresses.Where(x => x.EmailAddressType == EmailAddressType.To).Should().HaveCount(2);
        readModel.EmailAddresses.Should().ContainSingle(x => x.EmailAddressType == EmailAddressType.Cc);
        readModel.EmailAddresses.Should().ContainSingle(x => x.EmailAddressType == EmailAddressType.Bcc);
    }
}