using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Validators.CatchAllSender;

internal class UnassignUserFromCatchAllSenderCommandValidator : AbstractValidator<UnassignUserFromCatchAllSenderCommand>
{
    public UnassignUserFromCatchAllSenderCommandValidator()
    {
        this.RuleFor(x => x.SenderAddressId)
            .NotEmpty();

        this.RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
