using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/subdomains/gat-by-id")]
public record GetDetailedSubdomainByIdQuery(Guid SubdomainId) : IQuery<GetDetailedSubdomainByIdQueryResult>;