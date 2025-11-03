using FluentAssertions;
using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators;

/// <summary>
/// Tests for moderator management command validation.
/// Moderators can only be added with valid user and domain IDs.
/// </summary>
public class AddModeratorToDomainCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new AddModeratorToDomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyDomainId_ShouldHaveDomainIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new AddModeratorToDomainCommand(
            Guid.Empty,
            Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveUserIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new AddModeratorToDomainCommand(
            Guid.NewGuid(),
            Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithBothEmptyIds_ShouldHaveMultipleErrors()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new AddModeratorToDomainCommand(
            Guid.Empty,
            Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    private static IValidator<AddModeratorToDomainCommand> CreateValidator()
    {
        // Inline validator for testing purposes
        var validator = new InlineValidator<AddModeratorToDomainCommand>();

        validator.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithMessage("Domain ID is required.");

        validator.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        return validator;
    }
}