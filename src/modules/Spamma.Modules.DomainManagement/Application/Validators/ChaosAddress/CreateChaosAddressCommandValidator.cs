using FluentValidation;
using FluentValidation.Validators;
using Marten;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

internal class CreateChaosAddressCommandValidator : AbstractValidator<CreateChaosAddressCommand>
{
    private readonly IDocumentSession documentSession;

    public CreateChaosAddressCommandValidator(IDocumentSession documentSession)
    {
        this.documentSession = documentSession;

        this.RuleFor(x => x.ChaosAddressId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("ID is required.");

        this.RuleFor(x => x.DomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Domain ID is required.");

        this.RuleFor(x => x.SubdomainId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Subdomain ID is required.");

        this.RuleFor(x => x.LocalPart)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Local Part is required.")
            .MustAsync(this.IsValidEmailAddressAsync)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Local Part results in an invalid email address.");
    }

    private async Task<bool> IsValidEmailAddressAsync(CreateChaosAddressCommand command, string localPart, ValidationContext<CreateChaosAddressCommand> context, CancellationToken cancellationToken)
    {
        var subdomain = await this.documentSession.Query<SubdomainLookup>()
            .FirstOrDefaultAsync(s => s.Id == command.SubdomainId, cancellationToken);
        if (subdomain == null || string.IsNullOrWhiteSpace(subdomain.FullName))
        {
            return false;
        }

        var v = new AspNetCoreCompatibleEmailValidator<CreateChaosAddressCommand>();

        var emailAddress = $"{localPart}@{subdomain.FullName}";
        return v.IsValid(context, emailAddress);
    }
}
