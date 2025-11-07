using FluentAssertions;
using FluentValidation;
using Spamma.Modules.EmailInbox.Application.Validators.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators.Campaign;

public class DeleteCampaignValidatorTests
{
    private readonly DeleteCampaignValidator _validator;

    public DeleteCampaignValidatorTests()
    {
        this._validator = new DeleteCampaignValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        var command = new DeleteCampaignCommand(Guid.NewGuid());

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyCampaignId_Fails()
    {
        var command = new DeleteCampaignCommand(Guid.Empty);

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "CampaignId");
    }
}
