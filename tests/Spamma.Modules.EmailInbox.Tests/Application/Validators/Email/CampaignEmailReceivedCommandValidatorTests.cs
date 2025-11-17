using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators.Email;

public class CampaignEmailReceivedCommandValidatorTests
{
    private readonly CampaignEmailReceivedCommandValidator _validator;

    public CampaignEmailReceivedCommandValidatorTests()
    {
        this._validator = new CampaignEmailReceivedCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyEmailId_Fails()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailId");
    }

    [Fact]
    public async Task Validate_WithEmptyDomainId_Fails()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            "Test Subject",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId");
    }

    [Fact]
    public async Task Validate_WithEmptySubdomainId_Fails()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            "Test Subject",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId");
    }

    [Fact]
    public async Task Validate_WithEmptyCampaignId_Fails()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTimeOffset.UtcNow,
            Guid.Empty,
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CampaignId");
    }

    [Fact]
    public async Task Validate_WithEmptySubject_Fails()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Fact]
    public async Task Validate_WithSubjectExceeding500Characters_Fails()
    {
        // Arrange
        var longSubject = new string('a', 501);
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            longSubject,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            new[] { new EmailAddress("test@example.com", "Test User", EmailAddressType.To) });

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject" && e.ErrorMessage.Contains("500"));
    }

    [Fact]
    public async Task Validate_WithEmptyEmailAddresses_Fails()
    {
        // Arrange
        var command = new CampaignEmailReceivedCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Array.Empty<EmailAddress>());

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailAddresses");
    }
}
