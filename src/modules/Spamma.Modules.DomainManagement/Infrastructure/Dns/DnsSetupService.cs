using Marten;
using Microsoft.AspNetCore.DataProtection;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

internal class DnsSetupService(IDocumentSession session, IDataProtectionProvider protectionProvider, IEnumerable<IDnsRecordProvider> providers)
{
    public async Task<DnsSetupResult> EnsureAsync(Guid? integrationId, string parentDomain, DnsRecordSpec record, CancellationToken cancellationToken)
    {
        if (!integrationId.HasValue)
        {
            return new(DnsSetupStatus.NotConfigured, "This domain uses manual DNS setup. Add the displayed record at your DNS provider.");
        }

        var integration = await session.LoadAsync<DnsIntegration>(integrationId.Value, cancellationToken);
        if (integration is null || !string.Equals(Normalize(integration.ZoneName), Normalize(parentDomain), StringComparison.OrdinalIgnoreCase))
        {
            return new(DnsSetupStatus.NotConfigured, "The selected DNS integration is unavailable or does not match this domain.");
        }

        if (!string.Equals(Normalize(record.Name), Normalize(parentDomain), StringComparison.OrdinalIgnoreCase) &&
            !Normalize(record.Name).EndsWith($".{Normalize(parentDomain)}", StringComparison.OrdinalIgnoreCase))
        {
            return new(DnsSetupStatus.Failed, "The requested DNS record is outside the configured domain.");
        }

        var provider = providers.SingleOrDefault(x => string.Equals(x.Name, integration.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            return new(DnsSetupStatus.NotConfigured, "The selected DNS provider is unavailable.");
        }

        string credential;
        try
        {
            credential = protectionProvider.CreateProtector("Spamma.DomainManagement.DnsCredential.v1")
                .Unprotect(integration.ProtectedCredential);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return new(DnsSetupStatus.Failed, "The DNS integration credential cannot be read. Reconfigure the integration.");
        }

        var connection = new DnsConnection
        {
            Provider = integration.Provider,
            ZoneName = integration.ZoneName,
            ZoneId = integration.ZoneId,
            Credential = credential,
        };
        return await provider.EnsureAsync(connection, record, cancellationToken);
    }

    private static string Normalize(string name) => name.Trim().TrimEnd('.');
}
