using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class RevokeUserPasskeyCommandValidatorTests
{
    private readonly RevokeUserPasskeyCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserIdAndPasskeyId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new RevokeUserPasskeyCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = new RevokeUserPasskeyCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithEmptyPasskeyId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = new RevokeUserPasskeyCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.PasskeyId)
            .WithErrorCode(CommonValidationCodes.Required);
    }

    [Fact]
    public void Validate_WithEmptyBothIds_ShouldHaveTwoRequiredErrors()
    {
        // Arrange
        var command = new RevokeUserPasskeyCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(CommonValidationCodes.Required);
        result.ShouldHaveValidationErrorFor(x => x.PasskeyId)
            .WithErrorCode(CommonValidationCodes.Required);
    }
}
