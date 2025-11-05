using FluentAssertions;
using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Validators;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators;

/// <summary>
/// Tests for received email validation rules.
/// Emails must have valid domain, subdomain, and at least one recipient address.
/// </summary>
public class ReceivedEmailCommandValidatorTests
{
    private readonly ReceivedEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var emailAddresses = new[]
        {
            new ReceivedEmailCommand.EmailAddress("test@example.com", "Test User", EmailAddressType.To),
        };
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyDomainId_ShouldHaveDomainIdRequiredError()
    {
        // Arrange
        var emailAddresses = new[]
        {
            new ReceivedEmailCommand.EmailAddress("test@example.com", "Test User", EmailAddressType.To),
        };
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.Empty, // Empty DomainId
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId");
    }

    [Fact]
    public void Validate_WithEmptySubdomainId_ShouldHaveSubdomainIdRequiredError()
    {
        // Arrange
        var emailAddresses = new[]
        {
            new ReceivedEmailCommand.EmailAddress("test@example.com", "Test User", EmailAddressType.To),
        };
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty, // Empty SubdomainId
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId");
    }

    [Fact]
    public void Validate_WithEmptyEmailAddresses_ShouldHaveEmailAddressesRequiredError()
    {
        // Arrange
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            Array.Empty<ReceivedEmailCommand.EmailAddress>()); // Empty email addresses

        // Act
        var result = this._validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailAddresses");
    }

    [Fact]
    public void Validate_WithEmptyBothDomainAndSubdomain_ShouldHaveTwoErrors()
    {
        // Arrange
        var emailAddresses = new[]
        {
            new ReceivedEmailCommand.EmailAddress("test@example.com", "Test User", EmailAddressType.To),
        };
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.Empty, // Empty DomainId
            Guid.Empty, // Empty SubdomainId
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses);

        // Act
        var result = this._validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId");
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId");
    }
}
