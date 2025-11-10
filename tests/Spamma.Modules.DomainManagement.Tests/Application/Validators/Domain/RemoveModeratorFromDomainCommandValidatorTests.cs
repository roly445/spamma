using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Domain;

public class RemoveModeratorFromDomainCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new RemoveModeratorFromDomainCommandValidator();
        var command = new RemoveModeratorFromDomainCommand(
            DomainId: Guid.NewGuid(),
            UserId: Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyDomainId_HasValidationError()
    {
        // Arrange
        var validator = new RemoveModeratorFromDomainCommandValidator();
        var command = new RemoveModeratorFromDomainCommand(
            DomainId: Guid.Empty,
            UserId: Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DomainId)
            .WithErrorMessage("Domain ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyUserId_HasValidationError()
    {
        // Arrange
        var validator = new RemoveModeratorFromDomainCommandValidator();
        var command = new RemoveModeratorFromDomainCommand(
            DomainId: Guid.NewGuid(),
            UserId: Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required.");
    }
}
