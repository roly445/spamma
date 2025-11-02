using System;
using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ResultMonad;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DisableChaosAddress;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.DisableChaosAddress;

internal class DisableChaosAddressCommandHandler(
    IChaosAddressRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<DisableChaosAddressCommand>> validators,
    ILogger<DisableChaosAddressCommandHandler> logger) : CommandHandler<DisableChaosAddressCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(DisableChaosAddressCommand request, CancellationToken cancellationToken)
    {
        var chaosMaybe = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (chaosMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Chaos address with ID {request.Id} not found"));
        }

        var chaos = chaosMaybe.Value;
        var when = timeProvider.GetUtcNow().UtcDateTime;
        var result = chaos.Disable(when);
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await repository.SaveAsync(chaos, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}
