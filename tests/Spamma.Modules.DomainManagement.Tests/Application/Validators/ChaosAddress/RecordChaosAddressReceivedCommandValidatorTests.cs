using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.ChaosAddress;

public class RecordChaosAddressReceivedCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new RecordChaosAddressReceivedCommandValidator();
        var command = new RecordChaosAddressReceivedCommand(
            ChaosAddressId: Guid.NewGuid(),
            ReceivedAt: DateTime.UtcNow);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyChaosAddressId_HasValidationError()
    {
        // Arrange
        var validator = new RecordChaosAddressReceivedCommandValidator();
        var command = new RecordChaosAddressReceivedCommand(
            ChaosAddressId: Guid.Empty,
            ReceivedAt: DateTime.UtcNow);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChaosAddressId)
            .WithErrorMessage("Chaos Address Id is required.");
    }
}