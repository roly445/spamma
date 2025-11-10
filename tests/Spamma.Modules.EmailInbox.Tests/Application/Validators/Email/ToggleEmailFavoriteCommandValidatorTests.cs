using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators.Email;

public class ToggleEmailFavoriteCommandValidatorTests
{
    private readonly ToggleEmailFavoriteCommandValidator _validator;

    public ToggleEmailFavoriteCommandValidatorTests()
    {
        this._validator = new ToggleEmailFavoriteCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        var command = new ToggleEmailFavoriteCommand(Guid.NewGuid());

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyEmailId_Fails()
    {
        var command = new ToggleEmailFavoriteCommand(Guid.Empty);

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "EmailId");
    }
}
