using FluentValidation;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Validators.ChaosAddress;

internal class RecordChaosAddressReceivedCommandValidator : AbstractValidator<RecordChaosAddressReceivedCommand>
{
    public RecordChaosAddressReceivedCommandValidator()
    {
        this.RuleFor(x => x.ChaosAddressId)
            .NotEmpty()
            .WithErrorCode(CommonValidationCodes.Required)
            .WithMessage("Chaos Address Id is required.");

        this.RuleFor(x => x.ReceivedAt)
            .NotEqual(default(DateTime))
            .WithErrorCode(CommonValidationCodes.InvalidFormat)
            .WithMessage("ReceivedAt must be a valid timestamp.");
    }
}