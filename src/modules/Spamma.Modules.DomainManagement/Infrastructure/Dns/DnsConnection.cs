namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

internal class DnsConnection
{
    public string Provider { get; init; } = string.Empty;

    public string ZoneName { get; init; } = string.Empty;

    public string ZoneId { get; init; } = string.Empty;

    public string Credential { get; init; } = string.Empty;
}
