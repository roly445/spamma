using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.CreateChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

internal class CreateChaosAddressCommandValidator : AbstractValidator<CreateChaosAddressCommand>
{
    public CreateChaosAddressCommandValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
        this.RuleFor(x => x.DomainId).NotEmpty();
        this.RuleFor(x => x.SubdomainId).NotEmpty();
        this.RuleFor(x => x.LocalPart).NotEmpty();
    }
}
