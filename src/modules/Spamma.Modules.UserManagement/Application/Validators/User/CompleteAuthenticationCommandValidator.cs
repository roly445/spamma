using FluentValidation;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Validators.User;

public class CompleteAuthenticationCommandValidator : AbstractValidator<CompleteAuthenticationCommand>
{
    public CompleteAuthenticationCommandValidator()
    {
        this.RuleFor(x => x.UserId).NotEmpty();
        this.RuleFor(x => x.SecurityStamp).NotEmpty();
        this.RuleFor(x => x.AuthenticationAttemptId).NotEmpty();
    }
}