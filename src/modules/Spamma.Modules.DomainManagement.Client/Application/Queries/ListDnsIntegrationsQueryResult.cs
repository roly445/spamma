using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record ListDnsIntegrationsQueryResult(IReadOnlyList<DnsIntegrationSummary> Integrations) : IQueryResult;
