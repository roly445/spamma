using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Subdomain;

public class RemoveViewerFromSubdomainCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new RemoveViewerFromSubdomainCommandValidator();
        var command = new RemoveViewerFromSubdomainCommand(
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
        var validator = new RemoveViewerFromSubdomainCommandValidator();
        var command = new RemoveViewerFromSubdomainCommand(
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
        var validator = new RemoveViewerFromSubdomainCommandValidator();
        var command = new RemoveViewerFromSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            UserId: Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required.");
    }
}