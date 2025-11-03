using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EditChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

internal class EditChaosAddressCommandValidator : AbstractValidator<EditChaosAddressCommand>
{
    public EditChaosAddressCommandValidator()
    {
        // Basic validation: ids must be non-empty and localpart non-empty
    this.RuleFor(x => x.Id).NotEmpty();
    this.RuleFor(x => x.DomainId).NotEmpty();
    this.RuleFor(x => x.SubdomainId).NotEmpty();
    this.RuleFor(x => x.LocalPart).NotEmpty().MaximumLength(320); // local-part max length
    }
}
