using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DeleteChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

public class DeleteChaosAddressCommandValidator : AbstractValidator<DeleteChaosAddressCommand>
{
    public DeleteChaosAddressCommandValidator()
    {
        // Minimal validator: Id must be provided
        this.RuleFor(x => x.Id).NotEmpty();
    }
}
