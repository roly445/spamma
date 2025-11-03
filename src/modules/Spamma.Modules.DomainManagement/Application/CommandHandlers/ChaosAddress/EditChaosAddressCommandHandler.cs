using System;
using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ResultMonad;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EditChaosAddress;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.EditChaosAddress;

internal class EditChaosAddressCommandHandler(
    IChaosAddressRepository repository,
    IEnumerable<IValidator<EditChaosAddressCommand>> validators,
    ILogger<EditChaosAddressCommandHandler> logger) : CommandHandler<EditChaosAddressCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(EditChaosAddressCommand request, CancellationToken cancellationToken)
    {
        var chaosMaybe = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (chaosMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Chaos address with ID {request.Id} not found"));
        }

        var chaos = chaosMaybe.Value;
        var result = chaos.Edit(request.LocalPart, request.ConfiguredSmtpCode);
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await repository.SaveAsync(chaos, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}
