using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domain-management/dns-integrations/list")]
public record ListDnsIntegrationsQuery : IQuery<ListDnsIntegrationsQueryResult>;
