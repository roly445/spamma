using FluentValidation;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Validators;

/// <summary>
/// Validator for CompleteAuthenticationCommand.
/// This is a public authentication endpoint.
/// </summary>
public class CompleteAuthenticationCommandValidator : AbstractValidator<CompleteAuthenticationCommand>
{
    public CompleteAuthenticationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SecurityStamp).NotEmpty();
        RuleFor(x => x.AuthenticationAttemptId).NotEmpty();
    }
}
