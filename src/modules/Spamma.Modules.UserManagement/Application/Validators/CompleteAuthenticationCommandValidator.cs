using FluentValidation;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Validators;

public class CompleteAuthenticationCommandValidator : AbstractValidator<CompleteAuthenticationCommand>
{
    public CompleteAuthenticationCommandValidator()
    {
        this.RuleFor(x => x.UserId).NotEmpty();
        this.RuleFor(x => x.SecurityStamp).NotEmpty();
        this.RuleFor(x => x.AuthenticationAttemptId).NotEmpty();
    }
}