using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.CatchAllSender;

internal class UnassignUserFromCatchAllSenderCommandHandler(
    ICatchAllSenderAddressRepository repository,
    IEnumerable<IValidator<UnassignUserFromCatchAllSenderCommand>> validators,
    ILogger<UnassignUserFromCatchAllSenderCommandHandler> logger)
    : CommandHandler<UnassignUserFromCatchAllSenderCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UnassignUserFromCatchAllSenderCommand request, CancellationToken cancellationToken)
    {
        var addressMaybe = await repository.GetByIdAsync(request.SenderAddressId, cancellationToken);

        if (addressMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Catch-all sender address with ID {request.SenderAddressId} not found"));
        }

        var address = addressMaybe.Value;
        var unassignResult = address.UnassignUser(request.UserId);

        if (unassignResult.IsFailure)
        {
            return CommandResult.Failed(unassignResult.Error);
        }

        var saveResult = await repository.SaveAsync(address, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}
