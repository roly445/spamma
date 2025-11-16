using FluentValidation;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Validators;

public class StartAuthenticationCommandValidator : AbstractValidator<StartAuthenticationCommand>
{
    public StartAuthenticationCommandValidator()
    {
        this.RuleFor(x => x.EmailAddress)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Invalid email address format")
            .MaximumLength(255).WithMessage("Email address must not exceed 255 characters");
    }
}