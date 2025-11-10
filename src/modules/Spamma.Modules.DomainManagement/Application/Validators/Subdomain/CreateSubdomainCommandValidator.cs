using FluentValidation;
using Marten;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.DomainManagement.Infrastructure.Services;

namespace Spamma.Modules.DomainManagement.Application.Validators.Subdomain;

public class CreateSubdomainCommandValidator : AbstractValidator<CreateSubdomainCommand>
{
    private readonly IDocumentSession _documentSession;
    private readonly IDomainParserService _domainParserService;

    public CreateSubdomainCommandValidator(IDocumentSession documentSession, IDomainParserService domainParserService)
    {
        this._documentSession = documentSession;
        this._domainParserService = domainParserService;

        this.RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain name is required.")
            .MaximumLength(255)
            .WithMessage("Subdomain name must not exceed 255 characters.");

        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");

        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithMessage("Subdomain ID is required.");

        this.RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        this.RuleFor(x => x)
            .MustAsync(this.IsValidSubdomainAsync)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Subdomain must form a valid registrable domain when combined with its parent domain.");
    }

    private async Task<bool> IsValidSubdomainAsync(CreateSubdomainCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return false;
        }

        var domain = await this._documentSession.Query<DomainLookup>().FirstOrDefaultAsync(x => x.Id == command.DomainId, cancellationToken);
        if (domain == null || string.IsNullOrWhiteSpace(domain.DomainName))
        {
            return false;
        }

        var fullSubdomainName = $"{command.Name}.{domain.DomainName}";
        return this._domainParserService.IsValidDomain(fullSubdomainName);
    }
}