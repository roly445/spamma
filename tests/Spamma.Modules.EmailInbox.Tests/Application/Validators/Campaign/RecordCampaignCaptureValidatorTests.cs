using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Validators.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators.Campaign;

public class RecordCampaignCaptureValidatorTests
{
    private readonly RecordCampaignCaptureValidator _validator;

    public RecordCampaignCaptureValidatorTests()
    {
        this._validator = new RecordCampaignCaptureValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        var command = new RecordCampaignCaptureCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "campaign-value",
            DateTimeOffset.UtcNow);

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptySubdomainId_Fails()
    {
        var command = new RecordCampaignCaptureCommand(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            "campaign-value",
            DateTimeOffset.UtcNow);

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "SubdomainId");
    }

    [Fact]
    public async Task Validate_WithCampaignValueExceedingMaxLength_Fails()
    {
        var longCampaignValue = new string('a', 256);
        var command = new RecordCampaignCaptureCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            longCampaignValue,
            DateTimeOffset.UtcNow);

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "CampaignValue");
    }
}

