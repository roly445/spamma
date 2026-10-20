using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Validators.Subdomain;

public class SetupSubdomainDnsCommandValidator : AbstractValidator<SetupSubdomainDnsCommand>
{
    public SetupSubdomainDnsCommandValidator() => this.RuleFor(x => x.SubdomainId).NotEmpty();
}
