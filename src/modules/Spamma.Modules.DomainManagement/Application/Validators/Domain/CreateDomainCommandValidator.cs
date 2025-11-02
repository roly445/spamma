using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Infrastructure.Services;

namespace Spamma.Modules.DomainManagement.Application.Validators;

public class CreateDomainCommandValidator : AbstractValidator<CreateDomainCommand>
{
    private readonly IDomainParserService _domainParserService;

    public CreateDomainCommandValidator(IDomainParserService domainParserService)
    {
        this._domainParserService = domainParserService ?? throw new ArgumentNullException(nameof(domainParserService));

        this.RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain name is required.")
            .MaximumLength(255)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Domain name must not exceed 255 characters.")
            .Must(this.IsValidDomain)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Domain name must be a valid registrable domain with a recognized TLD.");

        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");

        this.RuleFor(x => x.PrimaryContactEmail)
            .EmailAddress()
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .When(x => !string.IsNullOrEmpty(x.PrimaryContactEmail))
            .WithMessage("Primary contact email must be a valid email address.");

        this.RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 1000 characters.");
    }

    private bool IsValidDomain(string domainName)
    {
        return this._domainParserService.IsValidDomain(domainName);
    }
}
