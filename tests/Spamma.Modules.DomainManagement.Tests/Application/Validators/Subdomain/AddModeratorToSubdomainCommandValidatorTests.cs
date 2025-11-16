using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Subdomain;

public class AddModeratorToSubdomainCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new AddModeratorToSubdomainCommandValidator();
        var command = new AddModeratorToSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            UserId: Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptySubdomainId_HasValidationError()
    {
        // Arrange
        var validator = new AddModeratorToSubdomainCommandValidator();
        var command = new AddModeratorToSubdomainCommand(
            SubdomainId: Guid.Empty,
            UserId: Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubdomainId)
            .WithErrorMessage("Subdomain ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyUserId_HasValidationError()
    {
        // Arrange
        var validator = new AddModeratorToSubdomainCommandValidator();
        var command = new AddModeratorToSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            UserId: Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required.");
    }
}