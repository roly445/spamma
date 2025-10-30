using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Validators;

/// <summary>
/// Validator for RegisterPasskeyCommand to ensure credential uniqueness and valid input.
/// </summary>
internal class RegisterPasskeyCommandValidator : AbstractValidator<RegisterPasskeyCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterPasskeyCommandValidator"/> class.
    /// </summary>
    /// <param name="passkeyRepository">Repository for checking existing passkeys.</param>
    public RegisterPasskeyCommandValidator(IPasskeyRepository passkeyRepository)
    {
        this.RuleFor(x => x.CredentialId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Credential ID is required.")
            .MustAsync(async (credentialId, token) =>
            {
                var existingPasskey = await passkeyRepository.GetByCredentialIdAsync(credentialId, token);
                return !existingPasskey.HasValue;
            })
            .WithErrorCode(CommonValidationCodes.NotUnique)
            .WithMessage("This credential is already registered by another user.")
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Credential ID is required.");

        this.RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Display name is required.")
            .MaximumLength(100)
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Display name must not exceed 100 characters.");

        this.RuleFor(x => x.Algorithm)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Algorithm is required.");

        this.RuleFor(x => x.PublicKey)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Public key is required.");
    }
}