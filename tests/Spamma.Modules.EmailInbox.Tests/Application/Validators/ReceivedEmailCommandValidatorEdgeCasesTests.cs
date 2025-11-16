using FluentAssertions;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators;

public class ReceivedEmailCommandValidatorEdgeCasesTests
{
    private readonly ReceivedEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyDomainId_ReturnsInvalid()
    {
        // Arrange
        var emailAddresses = new List<EmailAddress>
        {
            new("recipient@example.com", "Recipient", EmailAddressType.To),
        };

        var command = new ReceivedEmailCommand(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.Empty,
            SubdomainId: Guid.NewGuid(),
            Subject: "Test",
            WhenSent: DateTime.UtcNow,
            EmailAddresses: emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "DomainId" &&
            e.ErrorCode == CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithEmptySubdomainId_ReturnsInvalid()
    {
        // Arrange
        var emailAddresses = new List<EmailAddress>
        {
            new("recipient@example.com", "Recipient", EmailAddressType.To),
        };

        var command = new ReceivedEmailCommand(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.Empty,
            Subject: "Test",
            WhenSent: DateTime.UtcNow,
            EmailAddresses: emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "SubdomainId" &&
            e.ErrorCode == CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange - empty GUIDs and empty collection
        var command = new ReceivedEmailCommand(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.Empty,
            SubdomainId: Guid.Empty,
            Subject: "Test",
            WhenSent: DateTime.UtcNow,
            EmailAddresses: Array.Empty<EmailAddress>());

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId");
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId");
        result.Errors.Should().Contain(e => e.PropertyName == "EmailAddresses");
    }

    [Fact]
    public void Validate_WithSingleEmailAddress_ReturnsValid()
    {
        // Arrange - minimum valid scenario
        var emailAddresses = new List<EmailAddress>
        {
            new("to@example.com", "To User", EmailAddressType.To),
        };

        var command = new ReceivedEmailCommand(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            Subject: "Test",
            WhenSent: DateTime.UtcNow,
            EmailAddresses: emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleEmailAddresses_ReturnsValid()
    {
        // Arrange - complex scenario with multiple recipients
        var emailAddresses = new List<EmailAddress>
        {
            new("to1@example.com", "To User 1", EmailAddressType.To),
            new("to2@example.com", "To User 2", EmailAddressType.To),
            new("cc@example.com", "CC User", EmailAddressType.Cc),
            new("bcc@example.com", "BCC User", EmailAddressType.Bcc),
        };

        var command = new ReceivedEmailCommand(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            Subject: "Test",
            WhenSent: DateTime.UtcNow,
            EmailAddresses: emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}