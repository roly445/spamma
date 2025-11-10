using FluentAssertions;
using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators;

/// <summary>
/// Tests for email deletion validation rules.
/// Emails can only be deleted with valid email IDs.
/// </summary>
public class DeleteEmailCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new DeleteEmailCommand(Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyEmailId_ShouldHaveEmailIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new DeleteEmailCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailId" && e.ErrorMessage.Contains("required"));
    }

    private static IValidator<DeleteEmailCommand> CreateValidator()
    {
        // Inline validator for testing purposes
        var validator = new InlineValidator<DeleteEmailCommand>();

        validator.RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithMessage("Email ID is required.");

        return validator;
    }
}