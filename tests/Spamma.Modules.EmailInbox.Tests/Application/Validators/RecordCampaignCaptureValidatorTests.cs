using FluentAssertions;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Validators.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators;

public class RecordCampaignCaptureValidatorTests
{
    private readonly RecordCampaignCaptureValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "valid-campaign",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptySubdomainId_ReturnsInvalid()
    {
        // Arrange
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.Empty,
            MessageId: Guid.NewGuid(),
            CampaignValue: "campaign",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "SubdomainId" &&
            e.ErrorCode == CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithEmptyMessageId_ReturnsInvalid()
    {
        // Arrange
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.Empty,
            CampaignValue: "campaign",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MessageId" &&
            e.ErrorCode == CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithEmptyStringCampaignValue_ReturnsInvalid()
    {
        // Arrange
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: string.Empty,
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "CampaignValue" &&
            e.ErrorCode == CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyCampaignValue_ReturnsInvalid()
    {
        // Arrange
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "   ",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "CampaignValue" &&
            e.ErrorCode == CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithCampaignValueExactly255Characters_ReturnsValid()
    {
        // Arrange - exactly at boundary
        var campaignValue = new string('a', 255);
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: campaignValue,
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCampaignValueExceeding255Characters_ReturnsInvalid()
    {
        // Arrange - 256 characters (one over limit)
        var campaignValue = new string('a', 256);
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: campaignValue,
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "CampaignValue" &&
            e.ErrorMessage.Contains("255"));
    }

    [Fact]
    public void Validate_WithCampaignValue1000Characters_ReturnsInvalid()
    {
        // Arrange - way over limit
        var campaignValue = new string('x', 1000);
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: campaignValue,
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "CampaignValue" &&
            e.ErrorMessage.Contains("255"));
    }

    [Fact]
    public void Validate_WithSingleCharacterCampaignValue_ReturnsValid()
    {
        // Arrange - minimum valid length
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "x",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange - multiple violations
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.Empty,
            MessageId: Guid.Empty,
            CampaignValue: new string('a', 300),
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act
        var result = this._validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId");
        result.Errors.Should().Contain(e => e.PropertyName == "MessageId");
        result.Errors.Should().Contain(e => e.PropertyName == "CampaignValue");
    }
}