using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Validators.Email;

public class CampaignEmailReceivedCommandValidator : AbstractValidator<CampaignEmailReceivedCommand>
{
    public CampaignEmailReceivedCommandValidator()
    {
        this.RuleFor(x => x.EmailId).NotEmpty();
        this.RuleFor(x => x.DomainId).NotEmpty();
        this.RuleFor(x => x.SubdomainId).NotEmpty();
        this.RuleFor(x => x.CampaignId).NotEmpty();
        this.RuleFor(x => x.Subject).NotNull().MaximumLength(500);
        this.RuleFor(x => x.EmailAddresses).NotNull().NotEmpty();
    }
}