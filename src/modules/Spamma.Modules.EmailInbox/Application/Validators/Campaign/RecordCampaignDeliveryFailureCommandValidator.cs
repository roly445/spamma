using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Validators.Campaign;

public class RecordCampaignDeliveryFailureCommandValidator : AbstractValidator<RecordCampaignDeliveryFailureCommand>
{
    public RecordCampaignDeliveryFailureCommandValidator()
    {
        this.RuleFor(x => x.DomainId).NotEmpty();
        this.RuleFor(x => x.SubdomainId).NotEmpty();
        this.RuleFor(x => x.CampaignValue).NotEmpty().MaximumLength(255);
        this.RuleFor(x => x.FailureId).NotEmpty();
        this.RuleFor(x => x.AttemptId).NotEmpty();
        this.RuleFor(x => x.SmtpCode).InclusiveBetween(400, 599);
    }
}
