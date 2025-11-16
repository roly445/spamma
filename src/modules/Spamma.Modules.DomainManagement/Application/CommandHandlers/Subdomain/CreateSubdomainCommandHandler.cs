using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using SubdomainAggregate = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

internal class CreateSubdomainCommandHandler(
    ISubdomainRepository repository,
    IEnumerable<IValidator<CreateSubdomainCommand>> validators,
    ILogger<CreateSubdomainCommandHandler> logger, TimeProvider timeProvider)
    : CommandHandler<CreateSubdomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(CreateSubdomainCommand request, CancellationToken cancellationToken)
    {
        var result = SubdomainAggregate.Create(request.SubdomainId, request.DomainId, request.Name, request.Description, timeProvider.GetUtcNow().UtcDateTime);

        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var subdomain = result.Value;
        var saveResult = await repository.SaveAsync(subdomain, cancellationToken);
        return !saveResult.IsSuccess ?
            CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) :
            CommandResult.Succeeded();
    }
}