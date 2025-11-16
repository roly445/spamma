using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Domain;

public class UnsuspendDomainCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new UnsuspendDomainCommandValidator();
        var command = new UnsuspendDomainCommand(DomainId: Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyDomainId_HasValidationError()
    {
        // Arrange
        var validator = new UnsuspendDomainCommandValidator();
        var command = new UnsuspendDomainCommand(DomainId: Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DomainId)
            .WithErrorMessage("Domain ID is required.");
    }
}