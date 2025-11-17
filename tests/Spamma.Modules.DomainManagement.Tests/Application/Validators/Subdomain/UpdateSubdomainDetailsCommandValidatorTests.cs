using FluentValidation.TestHelper;
using Spamma.Modules.DomainManagement.Application.Validators.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Validators.Subdomain;

public class UpdateSubdomainDetailsCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var validator = new UpdateSubdomainDetailsCommandValidator();
        var command = new UpdateSubdomainDetailsCommand(
            SubdomainId: Guid.NewGuid(),
            Description: "Test description");

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptySubdomainId_HasValidationError()
    {
        // Arrange
        var validator = new UpdateSubdomainDetailsCommandValidator();
        var command = new UpdateSubdomainDetailsCommand(
            SubdomainId: Guid.Empty,
            Description: "Test description");

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubdomainId)
            .WithErrorMessage("Subdomain ID is required.");
    }
}