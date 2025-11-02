using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domain-management/chaos-addresses/find")]
public record GetChaosAddressBySubdomainAndLocalPartQuery(Guid SubdomainId, string LocalPart) : IQuery<GetChaosAddressBySubdomainAndLocalPartQueryResult>;
