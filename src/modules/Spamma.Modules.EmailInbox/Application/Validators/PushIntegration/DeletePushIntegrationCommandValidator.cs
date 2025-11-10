using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Validators.PushIntegration;

/// <summary>
/// Validator for DeletePushIntegrationCommand.
/// </summary>
public class DeletePushIntegrationCommandValidator : AbstractValidator<DeletePushIntegrationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeletePushIntegrationCommandValidator"/> class.
    /// </summary>
    public DeletePushIntegrationCommandValidator()
    {
        this.RuleFor(x => x.IntegrationId)
            .NotEmpty();
    }
}