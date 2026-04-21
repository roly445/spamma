using FluentValidation;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Validators.Email;

internal class UpdateCatchAllModeCommandValidator : AbstractValidator<UpdateCatchAllModeCommand>
{
    public UpdateCatchAllModeCommandValidator()
    {
    }
}
