using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Validators.Email;

/// <summary>
/// Validator for CampaignEmailReceivedCommand.
/// This is an internal system command triggered by email processing.
/// </summary>
public class CampaignEmailReceivedCommandValidator : AbstractValidator<CampaignEmailReceivedCommand>
{
    public CampaignEmailReceivedCommandValidator()
    {
        RuleFor(x => x.EmailId).NotEmpty();
        RuleFor(x => x.DomainId).NotEmpty();
        RuleFor(x => x.SubdomainId).NotEmpty();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Subject).NotNull().MaximumLength(500);
        RuleFor(x => x.EmailAddresses).NotNull().NotEmpty();
    }
}
