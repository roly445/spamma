using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.CatchAllSender;

internal class AssignUserToCatchAllSenderCommandHandler(
    ICatchAllSenderAddressRepository repository,
    IEnumerable<IValidator<AssignUserToCatchAllSenderCommand>> validators,
    ILogger<AssignUserToCatchAllSenderCommandHandler> logger)
    : CommandHandler<AssignUserToCatchAllSenderCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AssignUserToCatchAllSenderCommand request, CancellationToken cancellationToken)
    {
        var addressMaybe = await repository.GetByIdAsync(request.SenderAddressId, cancellationToken);

        if (addressMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Catch-all sender address with ID {request.SenderAddressId} not found"));
        }

        var address = addressMaybe.Value;
        var assignResult = address.AssignUser(request.UserId);

        if (assignResult.IsFailure)
        {
            return CommandResult.Failed(assignResult.Error);
        }

        var saveResult = await repository.SaveAsync(address, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}
