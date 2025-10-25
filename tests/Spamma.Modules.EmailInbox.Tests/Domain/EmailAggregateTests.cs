using FluentAssertions;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class EmailAggregateTests
{
    [Fact]
    public void Create_WithValidData_CreatesEmailAndRaisesEvent()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
            new("cc@example.com", "CC User", EmailAddressType.Cc)
        };

        // Act
        var result = Email.Create(emailId, domainId, subdomainId, "Test Subject", now, emailAddresses);

        // Verify
        result.ShouldBeOk(email =>
        {
            email.Id.Should().Be(emailId);
            email.ShouldHaveRaisedEvent<EmailReceived>(e =>
            {
                e.EmailId.Should().Be(emailId);
                e.Subject.Should().Be("Test Subject");
                e.EmailAddresses.Should().HaveCount(2);
            });
        });
    }

    [Fact]
    public void Delete_WhenEmailNotDeleted_DeletesEmailAndRaisesEvent()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To)
            }).Value;

        var now = DateTime.UtcNow.AddSeconds(1);

        // Act
        var result = email.Delete(now);

        // Verify
        result.IsSuccess.Should().BeTrue();
        email.ShouldHaveRaisedEvent<EmailDeleted>(e =>
        {
            e.WhenDeleted.Should().Be(now);
        });
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ReturnsError()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To)
            }).Value;

        email.Delete(DateTime.UtcNow);

        // Act
        var result = email.Delete(DateTime.UtcNow.AddSeconds(1));

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithMultipleEmailAddresses_StoresAllAddresses()
    {
        // Arrange
        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("to@example.com", "To User", EmailAddressType.To),
            new("cc@example.com", "CC User", EmailAddressType.Cc),
            new("bcc@example.com", "BCC User", EmailAddressType.Bcc),
            new("from@example.com", "From User", EmailAddressType.From)
        };

        // Act
        var result = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Multi-recipient",
            DateTime.UtcNow,
            emailAddresses);

        // Verify
        result.ShouldBeOk(email =>
        {
            email.ShouldHaveRaisedEvent<EmailReceived>(e =>
            {
                e.EmailAddresses.Should().HaveCount(4);
                e.EmailAddresses.Should().Contain(ea => ea.EmailAddressType == EmailAddressType.To);
                e.EmailAddresses.Should().Contain(ea => ea.EmailAddressType == EmailAddressType.Cc);
                e.EmailAddresses.Should().Contain(ea => ea.EmailAddressType == EmailAddressType.Bcc);
                e.EmailAddresses.Should().Contain(ea => ea.EmailAddressType == EmailAddressType.From);
            });
        });
    }

    [Fact]
    public void Create_WithDifferentDomainAndSubdomainIds_StoresCorrectly()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To)
        };

        // Act
        var result = Email.Create(
            Guid.NewGuid(),
            domainId,
            subdomainId,
            "Subject",
            DateTime.UtcNow,
            emailAddresses);

        // Verify
        result.ShouldBeOk(email =>
        {
            email.ShouldHaveRaisedEvent<EmailReceived>(e =>
            {
                e.DomainId.Should().Be(domainId);
                e.SubdomainId.Should().Be(subdomainId);
            });
        });
    }
}
