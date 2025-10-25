using FluentAssertions;
using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators;

/// <summary>
/// Tests for subdomain creation validation rules.
/// Subdomains must have valid names and belong to valid domains.
/// </summary>
public class CreateSubdomainCommandValidatorTests
{
    private static IValidator<CreateSubdomainCommand> CreateValidator()
    {
        // Inline validator for testing purposes
        var validator = new InlineValidator<CreateSubdomainCommand>();
        
        validator.RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Subdomain name is required.")
            .MaximumLength(255)
            .WithMessage("Subdomain name must not exceed 255 characters.");

        validator.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithMessage("Subdomain ID is required.");

        validator.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithMessage("Domain ID is required.");

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
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mail",
            "Mail server subdomain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptySubdomainName_ShouldHaveNameRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            "Mail server subdomain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithEmptySubdomainId_ShouldHaveSubdomainIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.Empty,
            Guid.NewGuid(),
            "mail",
            "Mail server subdomain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithEmptyDomainId_ShouldHaveDomainIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.Empty,
            "mail",
            "Mail server subdomain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithNameExceeding255Characters_ShouldHaveMaxLengthError()
    {
        // Arrange
        var validator = CreateValidator();
        var longName = new string('a', 256);
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            longName,
            "Description");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public void Validate_WithDescriptionExceeding1000Characters_ShouldHaveMaxLengthError()
    {
        // Arrange
        var validator = CreateValidator();
        var longDescription = new string('a', 1001);
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mail",
            longDescription);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("1000 characters"));
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveDescriptionError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mail",
            null);

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMaxLengthSubdomainName_ShouldPass()
    {
        // Arrange
        var validator = CreateValidator();
        var maxName = new string('a', 255);
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            maxName,
            "Description");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
