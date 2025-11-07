using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Validators.Domain;

public class UpdateDomainDetailsCommandValidator : AbstractValidator<UpdateDomainDetailsCommand>
{
    public UpdateDomainDetailsCommandValidator()
    {
        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");

        this.RuleFor(x => x.PrimaryContactEmail)
            .EmailAddress()
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Primary contact email must be a valid email address.");
    }
}