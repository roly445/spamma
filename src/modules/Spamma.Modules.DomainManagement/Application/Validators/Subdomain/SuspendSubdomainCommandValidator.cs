using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Validators;

public class SuspendSubdomainCommandValidator : AbstractValidator<SuspendSubdomainCommand>
{
    public SuspendSubdomainCommandValidator()
    {
        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain ID is required.");
    }
}