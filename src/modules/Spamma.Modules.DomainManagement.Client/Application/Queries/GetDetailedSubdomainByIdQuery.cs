using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/subdomains/get-by-id")]
public record GetDetailedSubdomainByIdQuery(Guid SubdomainId) : IQuery<GetDetailedSubdomainByIdQueryResult>;