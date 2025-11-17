using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.ChaosAddress;

internal class CreateChaosAddressCommandHandler(
    IChaosAddressRepository repository,
    TimeProvider timeProvider,
    IEnumerable<IValidator<CreateChaosAddressCommand>> validators,
    ILogger<CreateChaosAddressCommandHandler> logger) : CommandHandler<CreateChaosAddressCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(CreateChaosAddressCommand request, CancellationToken cancellationToken)
    {
        var when = timeProvider.GetUtcNow().UtcDateTime;

        var createResult = DomainManagement.Domain.ChaosAddressAggregate.ChaosAddress.Create(request.ChaosAddressId, request.DomainId, request.SubdomainId, request.LocalPart, request.ConfiguredSmtpCode, when);
        if (createResult.IsFailure)
        {
            return CommandResult.Failed(createResult.Error);
        }

        var chaos = createResult.Value;
        var saveResult = await repository.SaveAsync(chaos, cancellationToken);
        return !saveResult.IsSuccess ?
            CommandResult.Failed(new BluQubeErrorData(Common.Client.Infrastructure.Constants.CommonErrorCodes.SavingChangesFailed)) :
            CommandResult.Succeeded();
    }
}