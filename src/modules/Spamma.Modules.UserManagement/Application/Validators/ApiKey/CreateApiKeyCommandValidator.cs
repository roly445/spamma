using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Application.Validators.ApiKey;

internal class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        this.RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("API key name is required.")
            .MaximumLength(100)
            .WithMessage("API key name must not exceed 100 characters.")
            .MinimumLength(1)
            .WithMessage("API key name must be at least 1 character.");
    }
}