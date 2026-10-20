using BluQube.Commands;
using FluentValidation;
using Marten;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Dns;

internal class CreateDnsIntegrationCommandHandler(
    IDocumentSession session,
    IDataProtectionProvider protectionProvider,
    TimeProvider timeProvider,
    IEnumerable<IValidator<CreateDnsIntegrationCommand>> validators,
    ILogger<CreateDnsIntegrationCommandHandler> logger)
    : CommandHandler<CreateDnsIntegrationCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(CreateDnsIntegrationCommand request, CancellationToken cancellationToken)
    {
        if (await session.LoadAsync<DnsIntegration>(request.IntegrationId, cancellationToken) is not null)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed, "A DNS integration with this ID already exists."));
        }

        var integration = new DnsIntegration
        {
            Id = request.IntegrationId,
            Name = request.Name.Trim(),
            Provider = request.Provider,
            ZoneName = request.ZoneName.Trim().TrimEnd('.').ToLowerInvariant(),
            ZoneId = request.ZoneId.Trim(),
            ProtectedCredential = protectionProvider.CreateProtector("Spamma.DomainManagement.DnsCredential.v1").Protect(request.Credential),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        session.Store(integration);
        await session.SaveChangesAsync(cancellationToken);
        return CommandResult.Succeeded();
    }
}
