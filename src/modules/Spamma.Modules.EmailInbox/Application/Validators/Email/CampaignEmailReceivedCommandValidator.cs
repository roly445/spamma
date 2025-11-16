using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Validators.Email;

public class CampaignEmailReceivedCommandValidator : AbstractValidator<CampaignEmailReceivedCommand>
{
    public CampaignEmailReceivedCommandValidator()
    {
        this.RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email Id cannot be empty");

        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain Id cannot be empty");

        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain Id cannot be empty");

        this.RuleFor(x => x.CampaignId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Campaign Id cannot be empty");

        this.RuleFor(x => x.Subject)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subject cannot be empty")
            .MaximumLength(500)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Subject cannot exceed 500 characters");

        this.RuleFor(x => x.EmailAddresses)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email Address cannot be empty");
    }
}