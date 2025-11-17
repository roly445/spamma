using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Subdomain;

public class SuspendSubdomainCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new SuspendSubdomainCommandValidator();
        var command = new SuspendSubdomainCommand(
            SubdomainId: Guid.NewGuid(),
            Reason: SubdomainSuspensionReason.PolicyViolation,
            Notes: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptySubdomainId_HasValidationError()
    {
        // Arrange
        var validator = new SuspendSubdomainCommandValidator();
        var command = new SuspendSubdomainCommand(
            SubdomainId: Guid.Empty,
            Reason: SubdomainSuspensionReason.PolicyViolation,
            Notes: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubdomainId)
            .WithErrorMessage("Subdomain ID is required.");
    }
}