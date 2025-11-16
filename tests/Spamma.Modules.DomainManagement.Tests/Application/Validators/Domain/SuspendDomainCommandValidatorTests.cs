using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Domain;

public class SuspendDomainCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new SuspendDomainCommandValidator();
        var command = new SuspendDomainCommand(
            DomainId: Guid.NewGuid(),
            Reason: DomainSuspensionReason.PolicyViolation,
            Notes: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyDomainId_HasValidationError()
    {
        // Arrange
        var validator = new SuspendDomainCommandValidator();
        var command = new SuspendDomainCommand(
            DomainId: Guid.Empty,
            Reason: DomainSuspensionReason.PolicyViolation,
            Notes: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DomainId)
            .WithErrorMessage("Domain ID is required.");
    }
}