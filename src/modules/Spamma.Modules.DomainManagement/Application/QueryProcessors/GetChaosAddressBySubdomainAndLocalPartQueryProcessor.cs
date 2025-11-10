using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class GetChaosAddressBySubdomainAndLocalPartQueryProcessor(IDocumentSession session) : IQueryProcessor<GetChaosAddressBySubdomainAndLocalPartQuery, GetChaosAddressBySubdomainAndLocalPartQueryResult>
{
    public Task<QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>> Handle(GetChaosAddressBySubdomainAndLocalPartQuery request, CancellationToken cancellationToken)
    {
        var match = session.Query<ChaosAddressLookup>()
            .FirstOrDefault(x => x.SubdomainId == request.SubdomainId && x.LocalPart.Equals(request.LocalPart, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return Task.FromResult(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Failed());
        }

        var summary = new GetChaosAddressBySubdomainAndLocalPartQueryResult(
            match.Id,
            match.SubdomainId,
            match.DomainId,
            match.LocalPart,
            match.ConfiguredSmtpCode,
            match.Enabled);
        return Task.FromResult(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Succeeded(summary));
    }
}
