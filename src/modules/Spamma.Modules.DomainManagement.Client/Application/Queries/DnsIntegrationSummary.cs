namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record DnsIntegrationSummary(Guid Id, string Name, string Provider, string ZoneName, string ZoneId);
