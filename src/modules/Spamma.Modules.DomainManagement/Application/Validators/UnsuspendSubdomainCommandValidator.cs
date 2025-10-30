using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Application.Validators;

public class UnsuspendSubdomainCommandValidator : AbstractValidator<UnsuspendSubdomainCommand>
{
    public UnsuspendSubdomainCommandValidator()
    {
        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain ID is required.");
    }
}