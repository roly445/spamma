using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Moq;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Application.Validators;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Infrastructure.Services;
using DomainAggregate = Spamma.Modules.DomainManagement.Domain.DomainAggregate;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators;

/// <summary>
/// Tests for subdomain creation validation rules.
/// Subdomains must have valid names and belong to valid domains.
/// </summary>
public class CreateSubdomainCommandValidatorTests
{
    [Fact]
    public async Task Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mail",
            "Mail server subdomain");

        // Act
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithEmptySubdomainName_ShouldHaveNameRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            "Mail server subdomain");

        // Act
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithEmptySubdomainId_ShouldHaveSubdomainIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.Empty,
            Guid.NewGuid(),
            "mail",
            "Mail server subdomain");

        // Act
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubdomainId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithEmptyDomainId_ShouldHaveDomainIdRequiredError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.Empty,
            "mail",
            "Mail server subdomain");

        // Act
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DomainId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithNameExceeding255Characters_ShouldHaveMaxLengthError()
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
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public async Task Validate_WithDescriptionExceeding1000Characters_ShouldHaveMaxLengthError()
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
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("1000 characters"));
    }

    [Fact]
    public async Task Validate_WithNullDescription_ShouldNotHaveDescriptionError()
    {
        // Arrange
        var validator = CreateValidator();
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mail",
            null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithValidLengthSubdomainName_ShouldPass()
    {
        // Arrange
        var validator = CreateValidator();
        var validName = "mail";
        var command = new CreateSubdomainCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            validName,
            "Description");

        // Act
        var result = await validator.ValidateAsync(command);

        // Verify
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    private static IValidator<CreateSubdomainCommand> CreateValidator()
    {
        var domainRepositoryMock = new Mock<IDomainRepository>(MockBehavior.Strict);
        domainRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, CancellationToken>((domainId, ct) =>
            {
                if (domainId == Guid.Empty)
                {
                    return Task.FromResult(Maybe<DomainAggregate.Domain>.Nothing);
                }

                var result = DomainAggregate.Domain.Create(
                    domainId,
                    "example.com",
                    "admin@example.com",
                    "Example domain",
                    DateTime.UtcNow);

                if (result.IsSuccess)
                {
                    return Task.FromResult(Maybe.From(result.Value));
                }

                return Task.FromResult(Maybe<DomainAggregate.Domain>.Nothing);
            });

        var domainParserServiceMock = new Mock<IDomainParserService>(MockBehavior.Strict);
        domainParserServiceMock
            .Setup(x => x.IsValidDomain(It.IsAny<string>()))
            .Returns<string>(domain =>
            {
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return false;
                }

                var parts = domain.Split('.');
                if (parts.Length < 2)
                {
                    return false;
                }

                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part) || part.Length > 63 || part.StartsWith('-') || part.EndsWith('-'))
                    {
                        return false;
                    }
                }

                var tld = parts[parts.Length - 1];
                return tld.Length >= 2 && !char.IsDigit(tld[0]);
            });

        return new CreateSubdomainCommandValidator(domainRepositoryMock.Object, domainParserServiceMock.Object);
    }
}