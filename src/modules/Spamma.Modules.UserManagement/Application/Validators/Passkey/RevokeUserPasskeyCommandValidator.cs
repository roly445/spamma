using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

namespace Spamma.Modules.UserManagement.Application.Validators.Passkey;

public class RevokeUserPasskeyCommandValidator :
    AbstractValidator<RevokeUserPasskeyCommand>
{
    public RevokeUserPasskeyCommandValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("User ID is required.");

        this.RuleFor(x => x.PasskeyId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Passkey ID is required.");
    }
}