using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Application.Validators.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class AuthenticateWithPasskeyCommandValidatorTests
{
    private readonly AuthenticateWithPasskeyCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCredentialId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = MakeCommand(new byte[] { 0x01, 0x02, 0x03 });

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCredentialId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = MakeCommand(Array.Empty<byte>());

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.CredentialId)
            .WithErrorCode(CommonValidationCodes.Required);
    }

    private static AuthenticateWithPasskeyCommand MakeCommand(byte[] credentialId) =>
        new(credentialId, 42, new byte[37], new byte[1], new byte[1], "challenge==", "https://localhost", "localhost");
}