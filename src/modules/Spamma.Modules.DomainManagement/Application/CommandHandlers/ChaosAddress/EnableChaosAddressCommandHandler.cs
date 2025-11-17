using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.ChaosAddress;

internal class EnableChaosAddressCommandHandler(
    IChaosAddressRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<EnableChaosAddressCommand>> validators,
    ILogger<EnableChaosAddressCommandHandler> logger) : CommandHandler<EnableChaosAddressCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(EnableChaosAddressCommand request, CancellationToken cancellationToken)
    {
        var chaosMaybe = await repository.GetByIdAsync(request.ChaosAddressId, cancellationToken);
        if (chaosMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Chaos address with ID {request.ChaosAddressId} not found"));
        }

        var chaos = chaosMaybe.Value;
        var when = timeProvider.GetUtcNow().UtcDateTime;
        var result = chaos.Enable(when);
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await repository.SaveAsync(chaos, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}