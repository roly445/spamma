using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Validators;

public class AuthenticateWithPasskeyCommandValidator :
    AbstractValidator<AuthenticateWithPasskeyCommand>
{
    public AuthenticateWithPasskeyCommandValidator()
    {
        this.RuleFor(x => x.CredentialId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Credential ID is required.");
    }
}