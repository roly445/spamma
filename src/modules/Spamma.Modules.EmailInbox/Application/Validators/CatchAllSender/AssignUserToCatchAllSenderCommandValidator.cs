using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Validators.CatchAllSender;

internal class AssignUserToCatchAllSenderCommandValidator : AbstractValidator<AssignUserToCatchAllSenderCommand>
{
    public AssignUserToCatchAllSenderCommandValidator()
    {
        this.RuleFor(x => x.SenderAddressId)
            .NotEmpty();

        this.RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
