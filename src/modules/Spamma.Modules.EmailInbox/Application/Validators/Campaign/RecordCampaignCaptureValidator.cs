using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Validators.Campaign;

public class RecordCampaignCaptureValidator : AbstractValidator<RecordCampaignCaptureCommand>
{
    public RecordCampaignCaptureValidator()
    {
        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("SubdomainId is required.");

        this.RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("MessageId is required.");

        this.RuleFor(x => x.CampaignValue)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("CampaignValue is required.")
            .MaximumLength(255)
            .WithMessage("CampaignValue must not exceed 255 characters.");
    }
}
