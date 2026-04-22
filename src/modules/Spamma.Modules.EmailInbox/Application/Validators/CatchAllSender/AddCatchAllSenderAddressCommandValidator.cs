using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Validators.CatchAllSender;

internal class AddCatchAllSenderAddressCommandValidator : AbstractValidator<AddCatchAllSenderAddressCommand>
{
    public AddCatchAllSenderAddressCommandValidator()
    {
        this.RuleFor(x => x.SenderAddress)
            .NotEmpty()
            .Must(address => address.Contains('@'))
            .WithMessage("SenderAddress must be a valid email address containing '@'.");
    }
}
