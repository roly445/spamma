using BluQube.Commands;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;
using DomainAggregate = Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

internal class CreateDomainCommandHandler(IDomainRepository repository, IDocumentSession session, IEnumerable<IValidator<CreateDomainCommand>> validators, ILogger<CreateDomainCommandHandler> logger, TimeProvider timeProvider)
    : CommandHandler<CreateDomainCommand, CreateDomainCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<CreateDomainCommandResult>> HandleInternal(CreateDomainCommand request, CancellationToken cancellationToken)
    {
        if (request.DnsIntegrationId.HasValue)
        {
            var integration = await session.LoadAsync<DnsIntegration>(request.DnsIntegrationId.Value, cancellationToken);
            if (integration is null || !integration.ZoneName.Equals(request.Name.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult<CreateDomainCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, "The selected DNS integration does not match this domain."));
            }
        }

        var domainResult = DomainAggregate.Create(request.DomainId, request.Name, request.PrimaryContactEmail,
            request.Description, timeProvider.GetUtcNow().UtcDateTime, request.DnsIntegrationId);

        if (domainResult.IsFailure)
        {
            return CommandResult<CreateDomainCommandResult>.Failed(domainResult.Error);
        }

        var domain = domainResult.Value;
        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ?
            CommandResult<CreateDomainCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) :
            CommandResult<CreateDomainCommandResult>.Succeeded(new CreateDomainCommandResult(domain.VerificationToken));
    }
}
