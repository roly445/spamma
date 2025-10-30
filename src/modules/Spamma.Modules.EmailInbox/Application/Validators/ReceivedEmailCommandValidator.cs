using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Validators;

public class ReceivedEmailCommandValidator : AbstractValidator<ReceivedEmailCommand>
{
    public ReceivedEmailCommandValidator()
    {
        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");

        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain ID is required.");

        this.RuleFor(x => x.EmailAddresses)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("At least one Email Address is required.");
    }
}