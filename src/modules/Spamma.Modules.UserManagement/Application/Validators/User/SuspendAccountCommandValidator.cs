using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Validators.User;

public class SuspendAccountCommandValidator :
    AbstractValidator<SuspendAccountCommand>
{
    public SuspendAccountCommandValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("User ID is required.");
    }
}