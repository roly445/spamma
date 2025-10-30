using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Validators;

public class DeleteEmailCommandValidator : AbstractValidator<DeleteEmailCommand>
{
    public DeleteEmailCommandValidator()
    {
        this.RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email ID is required.");
    }
}