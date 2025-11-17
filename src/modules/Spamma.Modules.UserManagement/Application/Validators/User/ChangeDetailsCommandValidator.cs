using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Validators.User;

internal class ChangeDetailsCommandValidator :
    AbstractValidator<ChangeDetailsCommand>
{
    public ChangeDetailsCommandValidator(IUserRepository userRepository)
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("User ID is required.");

        this.RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email Address is required.")
            .EmailAddress()
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Email address format is invalid.")
            .MustAsync(async (c, s, token) =>
            {
                var result = await userRepository.GetByEmailAddressAsync(s, token);
                return !result.HasValue || result.Value.Id == c.UserId;
            })
            .WithErrorCode(CommonValidationCodes.NotUnique)
            .WithMessage("Email address is already in use.");

        this.RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Name must not exceed 100 characters.");
    }
}