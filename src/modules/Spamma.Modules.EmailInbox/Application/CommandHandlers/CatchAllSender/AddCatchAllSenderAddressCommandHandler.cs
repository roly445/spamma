using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.CatchAllSender;

internal class AddCatchAllSenderAddressCommandHandler(
    ICatchAllSenderAddressRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<AddCatchAllSenderAddressCommand>> validators,
    ILogger<AddCatchAllSenderAddressCommandHandler> logger)
    : CommandHandler<AddCatchAllSenderAddressCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(AddCatchAllSenderAddressCommand request, CancellationToken cancellationToken)
    {
        var addressId = Guid.NewGuid();
        var createResult = CatchAllSenderAddress.Create(addressId, request.SenderAddress, timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return CommandResult.Failed(createResult.Error);
        }

        var saveResult = await repository.SaveAsync(createResult.Value, cancellationToken);
        if (saveResult.IsFailure)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }

        return CommandResult.Succeeded();
    }
}
