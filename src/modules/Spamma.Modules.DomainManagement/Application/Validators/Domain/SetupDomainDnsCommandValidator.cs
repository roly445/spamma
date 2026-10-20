using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Validators.Domain;

public class SetupDomainDnsCommandValidator : AbstractValidator<SetupDomainDnsCommand>
{
    public SetupDomainDnsCommandValidator() => this.RuleFor(x => x.DomainId).NotEmpty();
}
