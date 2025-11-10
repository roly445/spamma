using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Validators.Subdomain;

public class UpdateSubdomainDetailsCommandValidator : AbstractValidator<UpdateSubdomainDetailsCommand>
{
    public UpdateSubdomainDetailsCommandValidator()
    {
        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain ID is required.");
    }
}