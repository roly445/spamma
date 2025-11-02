using System;
using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ResultMonad;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.RecordChaosAddressReceived;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.RecordChaosAddressReceived;

internal class RecordChaosAddressReceivedCommandHandler(
    IChaosAddressRepository repository,
    IEnumerable<IValidator<RecordChaosAddressReceivedCommand>> validators,
    ILogger<RecordChaosAddressReceivedCommandHandler> logger) : CommandHandler<RecordChaosAddressReceivedCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RecordChaosAddressReceivedCommand request, CancellationToken cancellationToken)
    {
        var chaosMaybe = await repository.GetByIdAsync(request.ChaosAddressId, cancellationToken);
        if (chaosMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Chaos address with ID {request.ChaosAddressId} not found"));
        }

        var chaos = chaosMaybe.Value;
        var result = chaos.RecordReceive(request.ReceivedAt);
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await repository.SaveAsync(chaos, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}
