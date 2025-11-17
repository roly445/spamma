using FluentAssertions;
using FluentValidation;
using Moq;
using Spamma.Modules.DomainManagement.Application.Validators.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Infrastructure.Services;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators;

public class CreateDomainCommandValidatorTests
{
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
    public void Validate_WithInvalidDomainFormat_ShouldHaveDomainFormatError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateDomainCommand(
            Guid.NewGuid(),
            "invalid",
            "admin@example.com",
            "Example domain");

        // Act
        var result = validator.Validate(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("valid registrable domain"));
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

    private static IValidator<CreateDomainCommand> CreateValidator()
    {
        var domainParserServiceMock = new Mock<IDomainParserService>(MockBehavior.Strict);
        domainParserServiceMock
            .Setup(x => x.IsValidDomain(It.IsAny<string>()))
            .Returns<string>(domain =>
            {
                // Mock implementation: return true for valid domains like "example.com", "test.co.uk"
                // This simulates the Public Suffix List validation
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return false;
                }

                // Simple check: must have at least one dot and valid structure
                var parts = domain.Split('.');
                if (parts.Length < 2)
                {
                    return false;
                }

                // Both parts must have content
                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part) || part.Length > 63 || part.StartsWith('-') || part.EndsWith('-'))
                    {
                        return false;
                    }
                }

                // TLD must be at least 2 chars and not numeric
                var tld = parts[parts.Length - 1];
                return tld.Length >= 2 && !char.IsDigit(tld[0]);
            });

        return new CreateDomainCommandValidator(domainParserServiceMock.Object);
    }
}