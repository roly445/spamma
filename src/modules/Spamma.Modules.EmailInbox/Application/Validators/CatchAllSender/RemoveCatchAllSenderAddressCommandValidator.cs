using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Validators.CatchAllSender;

internal class RemoveCatchAllSenderAddressCommandValidator : AbstractValidator<RemoveCatchAllSenderAddressCommand>
{
    public RemoveCatchAllSenderAddressCommandValidator()
    {
        this.RuleFor(x => x.SenderAddressId)
            .NotEmpty();
    }
}
