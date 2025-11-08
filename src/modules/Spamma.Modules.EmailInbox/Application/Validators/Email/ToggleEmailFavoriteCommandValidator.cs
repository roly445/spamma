using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Validators.Email;

public class ToggleEmailFavoriteCommandValidator : AbstractValidator<ToggleEmailFavoriteCommand>
{
    public ToggleEmailFavoriteCommandValidator()
    {
        this.RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email ID is required.");
    }
}