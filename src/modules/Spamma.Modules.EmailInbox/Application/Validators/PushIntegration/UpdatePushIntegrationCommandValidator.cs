using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Validators.PushIntegration;

/// <summary>
/// Validator for UpdatePushIntegrationCommand.
/// </summary>
public class UpdatePushIntegrationCommandValidator : AbstractValidator<UpdatePushIntegrationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePushIntegrationCommandValidator"/> class.
    /// </summary>
    public UpdatePushIntegrationCommandValidator()
    {
        this.RuleFor(x => x.IntegrationId)
            .NotEmpty();

        this.RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name != null);

        this.RuleFor(x => x.FilterType)
            .IsInEnum();

        this.RuleFor(x => x.FilterValue)
            .MaximumLength(500)
            .When(x => x.FilterValue != null);
    }
}