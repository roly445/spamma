using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Validators.User;

public class StartAuthenticationCommandValidator : AbstractValidator<StartAuthenticationCommand>
{
    public StartAuthenticationCommandValidator()
    {
        this.RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email address is required")
            .EmailAddress()
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Invalid email address format")
            .MaximumLength(255)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Email address must not exceed 255 characters");
    }
}