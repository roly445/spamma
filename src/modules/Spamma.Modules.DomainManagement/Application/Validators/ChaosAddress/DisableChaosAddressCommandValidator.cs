using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DisableChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

internal class DisableChaosAddressCommandValidator : AbstractValidator<DisableChaosAddressCommand>
{
    public DisableChaosAddressCommandValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
    }
}
