using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Validators.Domain;

public class SuspendDomainCommandValidator : AbstractValidator<SuspendDomainCommand>
{
    public SuspendDomainCommandValidator()
    {
        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");
    }
}