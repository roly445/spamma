using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Validators.Domain;

public class RemoveModeratorFromDomainCommandValidator : AbstractValidator<RemoveModeratorFromDomainCommand>
{
    public RemoveModeratorFromDomainCommandValidator()
    {
        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");

        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("User ID is required.");
    }
}