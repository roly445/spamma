using FluentAssertions;
using FluentValidation;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators.Email;

public class DeleteEmailCommandValidatorTests
{
    private readonly DeleteEmailCommandValidator _validator;

    public DeleteEmailCommandValidatorTests()
    {
        this._validator = new DeleteEmailCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        var command = new DeleteEmailCommand(Guid.NewGuid());

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyEmailId_Fails()
    {
        var command = new DeleteEmailCommand(Guid.Empty);

        var result = await this._validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "EmailId");
    }
}
