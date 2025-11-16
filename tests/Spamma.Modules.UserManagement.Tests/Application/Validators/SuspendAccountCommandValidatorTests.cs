using FluentValidation.TestHelper;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class SuspendAccountCommandValidatorTests
{
    private readonly SuspendAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new SuspendAccountCommand(Guid.NewGuid(), AccountSuspensionReason.PolicyViolation, null);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveRequiredError()
    {
        // Arrange
        var command = new SuspendAccountCommand(Guid.Empty, AccountSuspensionReason.PolicyViolation, null);

        // Act
        var result = this._validator.TestValidate(command);

        // Verify
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(CommonValidationCodes.Required);
    }
}