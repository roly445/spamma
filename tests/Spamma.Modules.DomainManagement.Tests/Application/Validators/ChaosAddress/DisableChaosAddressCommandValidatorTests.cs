using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.ChaosAddress;

public class DisableChaosAddressCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new DisableChaosAddressCommandValidator();
        var command = new DisableChaosAddressCommand(ChaosAddressId: Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyChaosAddressId_HasValidationError()
    {
        // Arrange
        var validator = new DisableChaosAddressCommandValidator();
        var command = new DisableChaosAddressCommand(ChaosAddressId: Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChaosAddressId)
            .WithErrorMessage("ID is required.");
    }
}