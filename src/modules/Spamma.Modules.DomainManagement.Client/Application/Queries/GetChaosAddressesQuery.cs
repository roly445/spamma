using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domain-management/chaos-addresses")]
public record GetChaosAddressesQuery(Guid? SubdomainId = null, int PageNumber = 1, int PageSize = 50) : IQuery<GetChaosAddressesQueryResult>;
