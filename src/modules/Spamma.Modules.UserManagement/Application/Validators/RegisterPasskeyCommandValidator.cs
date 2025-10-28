using FluentValidation;
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
            .WithMessage("Credential ID is required.");

        this.RuleFor(x => x.CredentialId)
            .MustAsync(async (credentialId, token) =>
            {
                var existingPasskey = await passkeyRepository.GetByCredentialIdAsync(credentialId, token);
                return !existingPasskey.HasValue;
            })
            .When(x => x.CredentialId.Length > 0)
            .WithMessage("This credential is already registered by another user.");

        this.RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required.")
            .MaximumLength(100)
            .WithMessage("Display name must not exceed 100 characters.");

        this.RuleFor(x => x.Algorithm)
            .NotEmpty()
            .WithMessage("Algorithm is required.");

        this.RuleFor(x => x.PublicKey)
            .NotEmpty()
            .WithMessage("Public key is required.");
    }
}