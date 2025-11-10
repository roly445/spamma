using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Validators.PushIntegration;

/// <summary>
/// Validator for CreatePushIntegrationCommand.
/// </summary>
public class CreatePushIntegrationCommandValidator : AbstractValidator<CreatePushIntegrationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePushIntegrationCommandValidator"/> class.
    /// </summary>
    public CreatePushIntegrationCommandValidator()
    {
        this.RuleFor(x => x.SubdomainId)
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