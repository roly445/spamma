namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

public class DnsIntegration
{
    public Guid Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string ZoneName { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string ProtectedCredential { get; set; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
