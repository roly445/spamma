using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Infrastructure.Services;

namespace Spamma.Modules.DomainManagement.Application.Validators;

public class CreateSubdomainCommandValidator : AbstractValidator<CreateSubdomainCommand>
{
    private readonly IDomainRepository _domainRepository;
    private readonly IDomainParserService _domainParserService;

    public CreateSubdomainCommandValidator(IDomainRepository domainRepository, IDomainParserService domainParserService)
    {
        this._domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
        this._domainParserService = domainParserService ?? throw new ArgumentNullException(nameof(domainParserService));

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

        var domain = await this._domainRepository.GetByIdAsync(command.DomainId, cancellationToken);
        if (!domain.HasValue || string.IsNullOrWhiteSpace(domain.Value.Name))
        {
            return false;
        }

        var fullSubdomainName = $"{command.Name}.{domain.Value.Name}";
        return this._domainParserService.IsValidDomain(fullSubdomainName);
    }
}