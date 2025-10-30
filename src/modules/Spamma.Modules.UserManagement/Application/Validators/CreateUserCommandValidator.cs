﻿using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Validators;

internal class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        this.RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.");

        this.RuleFor(x => x.EmailAddress)
            .MustAsync(async (s, token) =>
            {
                var result = await userRepository.GetByEmailAddressAsync(s, token);
                return result.HasValue;
            })
            .WithMessage("Email address is already in use.")
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("Invalid email address format.");

        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("User ID is required.");
    }
}