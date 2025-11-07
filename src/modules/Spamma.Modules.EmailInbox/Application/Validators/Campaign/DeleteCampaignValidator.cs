using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Validators.Campaign;

public class DeleteCampaignValidator : AbstractValidator<DeleteCampaignCommand>
{
    public DeleteCampaignValidator()
    {
        this.RuleFor(x => x.CampaignId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("CampaignId is required.");
    }
}
