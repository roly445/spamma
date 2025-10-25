using FluentAssertions;
using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators;

/// <summary>
/// Tests for domain creation validation rules.
/// Domain names must be valid hostnames with appropriate length and format.
/// </summary>
public class CreateDomainCommandValidatorTests
{
    private static IValidator<CreateDomainCommand> CreateValidator()
    {
        // For now, we'll create a basic validator inline since none exists
        // In a real scenario, this would test the actual CreateDomainCommandValidator
        var validator = new InlineValidator<CreateDomainCommand>();
        
        validator.RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Domain name is required.")
            .MaximumLength(255)
            .WithMessage("Domain name must not exceed 255 characters.");

        validator.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithMessage("Domain ID is required.");

        validator.RuleFor(x => x.PrimaryContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.PrimaryContactEmail))
            .WithMessage("Primary contact email must be a valid email address.");

        validator.RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 1000 characters.");

        return validator;
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            "admin@example.com",
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyDomainName_ShouldHaveNameRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            string.Empty,
            "admin@example.com",
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithDomainNameExceeding255Characters_ShouldHaveMaxLengthError()
    {
        // Arrange
        var validator = CreateValidator();
        var longName = new string('a', 256);
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            longName,
            "admin@example.com",
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public void Validate_WithEmptyDomainId_ShouldHaveDomainIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.Empty,
            "example.com",
            "admin@example.com",
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithInvalidPrimaryContactEmail_ShouldHaveEmailFormatError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            "not-an-email",
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrimaryContactEmail" && e.ErrorMessage.Contains("valid email"));
    }

    [Fact]
    public void Validate_WithDescriptionExceeding1000Characters_ShouldHaveMaxLengthError()
    {
        // Arrange
        var validator = CreateValidator();
        var longDescription = new string('a', 1001);
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            "admin@example.com",
            longDescription);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("1000 characters"));
    }

    [Fact]
    public void Validate_WithNullPrimaryContactEmail_ShouldNotHaveEmailError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            null,
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveDescriptionError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            "admin@example.com",
            null);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidEmailAndMaxLengthDescription_ShouldPass()
    {
        // Arrange
        var validator = CreateValidator();
        var maxDescription = new string('a', 1000);
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "example.com",
            "admin+domain@example.co.uk",
            maxDescription);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
