using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domains/get-by-id")]
public record GetDetailedDomainByIdQuery(Guid DomainId) : IQuery<GetDetailedDomainByIdQueryResult>;