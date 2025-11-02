using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EnableChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

internal class EnableChaosAddressCommandValidator : AbstractValidator<EnableChaosAddressCommand>
{
    public EnableChaosAddressCommandValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
    }
}
