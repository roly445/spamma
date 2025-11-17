using FluentValidation.TestHelper;
using Moq;
using Spamma.Modules.DomainManagement.Application.Validators.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.DomainManagement.Infrastructure.Services;

namespace Spamma.Modules.DomainManagement.Tests.Integration.Validators;

public class CreateSubdomainCommandValidatorTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public CreateSubdomainCommandValidatorTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domainLookup = new DomainLookup
        {
            Id = domainId,
            DomainName = "example.com",
            CreatedAt = DateTime.UtcNow,
            IsVerified = true,
        };

        this._fixture.Session!.Store(domainLookup);
        await this._fixture.Session.SaveChangesAsync();

        var domainParserService = CreateMockDomainParserService(isValid: true);
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: domainId,
            Name: "mail",
            Description: "Mail subdomain");

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyName_HasValidationError()
    {
        // Arrange
        var domainParserService = new DomainParserService();
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session!, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            Name: string.Empty,
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Subdomain name is required.");
    }

    [Fact]
    public async Task ValidateAsync_NameTooLong_HasValidationError()
    {
        // Arrange
        var domainParserService = new DomainParserService();
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session!, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            Name: new string('a', 256),
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Subdomain name must not exceed 255 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyDomainId_HasValidationError()
    {
        // Arrange
        var domainParserService = new DomainParserService();
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session!, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: Guid.Empty,
            Name: "mail",
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DomainId)
            .WithErrorMessage("Domain ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptySubdomainId_HasValidationError()
    {
        // Arrange
        var domainParserService = new DomainParserService();
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session!, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.Empty,
            DomainId: Guid.NewGuid(),
            Name: "mail",
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubdomainId)
            .WithErrorMessage("Subdomain ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_DescriptionTooLong_HasValidationError()
    {
        // Arrange
        var domainParserService = new DomainParserService();
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session!, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            Name: "mail",
            Description: new string('a', 1001));

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 1000 characters.");
    }

    [Fact]
    public async Task ValidateAsync_InvalidSubdomainFormat_HasValidationError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var domainLookup = new DomainLookup
        {
            Id = domainId,
            DomainName = "example.com",
            CreatedAt = DateTime.UtcNow,
            IsVerified = true,
        };

        this._fixture.Session!.Store(domainLookup);
        await this._fixture.Session.SaveChangesAsync();

        var domainParserService = CreateMockDomainParserService(isValid: false);
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: domainId,
            Name: "invalid..subdomain",
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Subdomain must form a valid registrable domain when combined with its parent domain.");
    }

    [Fact]
    public async Task ValidateAsync_DomainNotFound_HasValidationError()
    {
        // Arrange - No domain stored in database
        var domainParserService = new DomainParserService();
        var validator = new CreateSubdomainCommandValidator(this._fixture.Session!, domainParserService);
        var command = new CreateSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            Name: "mail",
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Subdomain must form a valid registrable domain when combined with its parent domain.");
    }

    private static IDomainParserService CreateMockDomainParserService(bool isValid = true)
    {
        var mock = new Mock<IDomainParserService>();
        mock.Setup(x => x.IsValidDomain(It.IsAny<string>())).Returns(isValid);
        return mock.Object;
    }
}