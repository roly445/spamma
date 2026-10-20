using BluQube.Commands;
using FluentValidation;
using Marten;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Dns;

internal class UpdateDnsIntegrationCommandHandler(
    IDocumentSession session,
    IDataProtectionProvider protectionProvider,
    IEnumerable<IValidator<UpdateDnsIntegrationCommand>> validators,
    ILogger<UpdateDnsIntegrationCommandHandler> logger)
    : CommandHandler<UpdateDnsIntegrationCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(UpdateDnsIntegrationCommand request, CancellationToken cancellationToken)
    {
        var existing = await session.LoadAsync<DnsIntegration>(request.IntegrationId, cancellationToken);
        if (existing is null)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed, "DNS integration was not found."));
        }

        existing.Name = request.Name.Trim();
        existing.Provider = request.Provider;
        existing.ZoneName = request.ZoneName.Trim().TrimEnd('.').ToLowerInvariant();
        existing.ZoneId = request.ZoneId.Trim();
        if (!string.IsNullOrWhiteSpace(request.Credential))
        {
            existing.ProtectedCredential = protectionProvider.CreateProtector("Spamma.DomainManagement.DnsCredential.v1").Protect(request.Credential);
        }

        session.Store(existing);
        await session.SaveChangesAsync(cancellationToken);
        return CommandResult.Succeeded();
    }
}
