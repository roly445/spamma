using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Application.Validators.ApiKey;

internal class RevokeApiKeyCommandValidator : AbstractValidator<RevokeApiKeyCommand>
{
    public RevokeApiKeyCommandValidator()
    {
        this.RuleFor(x => x.ApiKeyId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("API key ID is required.");
    }
}