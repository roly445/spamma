using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Application.Validators;

public class VerifyDomainCommandValidator : AbstractValidator<VerifyDomainCommand>
{
    public VerifyDomainCommandValidator()
    {
        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");
    }
}