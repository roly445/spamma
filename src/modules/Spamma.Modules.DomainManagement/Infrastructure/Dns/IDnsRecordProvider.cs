namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

internal interface IDnsRecordProvider
{
    string Name { get; }

    Task<DnsSetupResult> EnsureAsync(DnsConnection connection, DnsRecordSpec record, CancellationToken cancellationToken);
}
