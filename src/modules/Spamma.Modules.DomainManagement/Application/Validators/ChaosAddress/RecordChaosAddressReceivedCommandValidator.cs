using System;
using FluentValidation;
using Spamma.Modules.DomainManagement.Client.Application.Commands.RecordChaosAddressReceived;

namespace Spamma.Modules.DomainManagement.Application.Validators;

internal class RecordChaosAddressReceivedCommandValidator : AbstractValidator<RecordChaosAddressReceivedCommand>
{
    public RecordChaosAddressReceivedCommandValidator()
    {
        this.RuleFor(x => x.ChaosAddressId)
            .NotEmpty()
            .WithMessage("ChaosAddressId is required.");

        this.RuleFor(x => x.ReceivedAt)
            .NotEqual(default(DateTime))
            .WithMessage("ReceivedAt must be a valid timestamp.");
    }
}
