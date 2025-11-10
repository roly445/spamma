using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Domain;

public class UpdateDomainDetailsCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new UpdateDomainDetailsCommandValidator();
        var command = new UpdateDomainDetailsCommand(
            DomainId: Guid.NewGuid(),
            PrimaryContactEmail: "admin@example.com",
            Description: "Test domain");

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyDomainId_HasValidationError()
    {
        // Arrange
        var validator = new UpdateDomainDetailsCommandValidator();
        var command = new UpdateDomainDetailsCommand(
            DomainId: Guid.Empty,
            PrimaryContactEmail: "admin@example.com",
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DomainId)
            .WithErrorMessage("Domain ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_InvalidEmailFormat_HasValidationError()
    {
        // Arrange
        var validator = new UpdateDomainDetailsCommandValidator();
        var command = new UpdateDomainDetailsCommand(
            DomainId: Guid.NewGuid(),
            PrimaryContactEmail: "not-an-email",
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PrimaryContactEmail)
            .WithErrorMessage("Primary contact email must be a valid email address.");
    }

    [Fact]
    public async Task ValidateAsync_NullEmail_IsValid()
    {
        // Arrange
        var validator = new UpdateDomainDetailsCommandValidator();
        var command = new UpdateDomainDetailsCommand(
            DomainId: Guid.NewGuid(),
            PrimaryContactEmail: null,
            Description: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert - Email is optional, so null should be valid
        result.ShouldNotHaveAnyValidationErrors();
    }
}
