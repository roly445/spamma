using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class UnsuspendAccountCommandValidatorTests
{
    private readonly UnsuspendAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new UnsuspendAccountCommand(Guid.NewGuid());

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = new UnsuspendAccountCommand(Guid.Empty);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(CommonValidationCodes.Required);
    }
}
