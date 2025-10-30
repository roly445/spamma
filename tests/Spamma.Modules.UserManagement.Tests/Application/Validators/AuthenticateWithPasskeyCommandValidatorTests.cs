using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class AuthenticateWithPasskeyCommandValidatorTests
{
    private readonly AuthenticateWithPasskeyCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCredentialId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new AuthenticateWithPasskeyCommand(new byte[] { 0x01, 0x02, 0x03 }, 42);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCredentialId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = new AuthenticateWithPasskeyCommand(Array.Empty<byte>(), 42);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.CredentialId)
            .WithErrorCode(CommonValidationCodes.Required);
    }
}
