using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Tests.Integration.Validators;

public class CreateChaosAddressCommandValidatorTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public CreateChaosAddressCommandValidatorTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var subdomainLookup = new SubdomainLookup
        {
            Id = subdomainId,
            DomainId = domainId,
            SubdomainName = "mail",
            FullName = "mail.example.com",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
        };

        this._fixture.Session!.Store(subdomainLookup);
        await this._fixture.Session.SaveChangesAsync();

        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.NewGuid(),
            DomainId: domainId,
            SubdomainId: subdomainId,
            LocalPart: "test",
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyChaosAddressId_HasValidationError()
    {
        // Arrange
        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session!);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.Empty,
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            LocalPart: "test",
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChaosAddressId)
            .WithErrorMessage("ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyDomainId_HasValidationError()
    {
        // Arrange
        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session!);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.NewGuid(),
            DomainId: Guid.Empty,
            SubdomainId: Guid.NewGuid(),
            LocalPart: "test",
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

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
        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session!);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.Empty,
            LocalPart: "test",
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubdomainId)
            .WithErrorMessage("Subdomain ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyLocalPart_HasValidationError()
    {
        // Arrange
        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session!);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            LocalPart: string.Empty,
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LocalPart)
            .WithErrorMessage("Local Part is required.");
    }

    [Fact]
    public async Task ValidateAsync_InvalidEmailFormat_HasValidationError()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var subdomainLookup = new SubdomainLookup
        {
            Id = subdomainId,
            DomainId = domainId,
            SubdomainName = "mail",
            FullName = "mail.example.com",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
        };

        this._fixture.Session!.Store(subdomainLookup);
        await this._fixture.Session.SaveChangesAsync();

        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.NewGuid(),
            DomainId: domainId,
            SubdomainId: subdomainId,
            LocalPart: "@invalid",
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LocalPart)
            .WithErrorMessage("Local Part results in an invalid email address.");
    }

    [Fact]
    public async Task ValidateAsync_SubdomainNotFound_HasValidationError()
    {
        // Arrange - No subdomain stored in database
        var validator = new CreateChaosAddressCommandValidator(this._fixture.Session!);
        var command = new CreateChaosAddressCommand(
            ChaosAddressId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            LocalPart: "test",
            ConfiguredSmtpCode: SmtpResponseCode.MailboxUnavailable);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LocalPart)
            .WithErrorMessage("Local Part results in an invalid email address.");
    }
}
