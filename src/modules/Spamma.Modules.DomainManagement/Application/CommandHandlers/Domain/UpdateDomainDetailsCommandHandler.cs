using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

public class UpdateDomainDetailsCommandHandler(
    IDomainRepository repository,
    IEnumerable<IValidator<UpdateDomainDetailsCommand>> validators,
    ILogger<UpdateDomainDetailsCommandHandler> logger)
    : CommandHandler<UpdateDomainDetailsCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UpdateDomainDetailsCommand request, CancellationToken cancellationToken)
    {
        var domainMaybe = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Domain with ID {request.DomainId} not found"));
        }

        var domain = domainMaybe.Value;
        var updateResult = domain.ChangeDetails(request.PrimaryContactEmail, request.Description);
        if (updateResult.IsFailure)
        {
            return CommandResult.Failed(updateResult.Error);
        }

        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}