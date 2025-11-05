using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Validators.Campaign;

public class DeleteCampaignValidator : AbstractValidator<DeleteCampaignCommand>
{
    public DeleteCampaignValidator()
    {
        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("SubdomainId is required.");

        this.RuleFor(x => x.CampaignValue)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("CampaignValue is required.")
            .MaximumLength(255)
            .WithMessage("CampaignValue must not exceed 255 characters.");
    }
}
