using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;

namespace Spamma.Modules.DomainManagement.Application.Validators.Dns;

public class CreateDnsIntegrationCommandValidator : AbstractValidator<CreateDnsIntegrationCommand>
{
    public CreateDnsIntegrationCommandValidator()
    {
        this.RuleFor(x => x.IntegrationId).NotEmpty();
        this.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        this.RuleFor(x => x.Provider).Equal("Cloudflare");
        this.RuleFor(x => x.ZoneName).NotEmpty().MaximumLength(255).Matches(@"^[A-Za-z0-9.-]+$");
        this.RuleFor(x => x.ZoneId).NotEmpty().Length(32).Matches("^[a-fA-F0-9]+$");
        this.RuleFor(x => x.Credential).NotEmpty().MaximumLength(1024);
    }
}
