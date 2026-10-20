using BluQube.Commands;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

internal class UpdateDomainDetailsCommandHandler(
    IDomainRepository repository,
    IDocumentSession session,
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
        if (request.DnsIntegrationId.HasValue)
        {
            var integration = await session.LoadAsync<DnsIntegration>(request.DnsIntegrationId.Value, cancellationToken);
            if (integration is null || !string.Equals(integration.ZoneName, domain.Name, StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, "The selected DNS integration does not match this domain."));
            }
        }

        var updateResult = domain.ChangeDetails(request.PrimaryContactEmail, request.Description);
        if (updateResult.IsFailure)
        {
            return CommandResult.Failed(updateResult.Error);
        }

        var dnsResult = domain.ChangeDnsIntegration(request.DnsIntegrationId);
        if (dnsResult.IsFailure)
        {
            return CommandResult.Failed(dnsResult.Error);
        }

        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }
}
