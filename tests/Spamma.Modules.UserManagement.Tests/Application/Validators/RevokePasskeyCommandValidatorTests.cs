using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Application.Validators.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class RevokePasskeyCommandValidatorTests
{
    private readonly RevokePasskeyCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidPasskeyId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new RevokePasskeyCommand(Guid.NewGuid());

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyPasskeyId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = new RevokePasskeyCommand(Guid.Empty);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.PasskeyId)
            .WithErrorCode(CommonValidationCodes.Required);
    }
}