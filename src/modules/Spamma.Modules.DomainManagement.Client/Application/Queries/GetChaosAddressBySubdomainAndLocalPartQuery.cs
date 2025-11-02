using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record GetChaosAddressBySubdomainAndLocalPartQuery(Guid SubdomainId, string LocalPart) : IQuery<GetChaosAddressBySubdomainAndLocalPartQueryResult>;
