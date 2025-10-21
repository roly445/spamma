using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domains/gat-by-id")]
public record GetDetailedDomainByIdQuery(Guid DomainId) : IQuery<GetDetailedDomainByIdQueryResult>;