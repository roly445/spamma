using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

public class DeleteChaosAddressCommandValidator : AbstractValidator<DeleteChaosAddressCommand>
{
    public DeleteChaosAddressCommandValidator()
    {
        this.RuleFor(x => x.ChaosAddressId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("ID is required.");
    }
}