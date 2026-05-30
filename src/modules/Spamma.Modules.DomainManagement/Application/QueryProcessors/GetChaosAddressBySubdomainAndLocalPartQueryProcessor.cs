using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class GetChaosAddressBySubdomainAndLocalPartQueryProcessor(IDocumentSession session) : IQueryProcessor<GetChaosAddressBySubdomainAndLocalPartQuery, GetChaosAddressBySubdomainAndLocalPartQueryResult>
{
    public ValueTask<QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>> Handle(GetChaosAddressBySubdomainAndLocalPartQuery request, CancellationToken cancellationToken)
    {
        var match = session.Query<ChaosAddressLookup>()
            .FirstOrDefault(x => x.SubdomainId == request.SubdomainId && x.LocalPart.Equals(request.LocalPart, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return ValueTask.FromResult(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Failed());
        }

        var summary = new GetChaosAddressBySubdomainAndLocalPartQueryResult(
            match.Id,
            match.SubdomainId,
            match.DomainId,
            match.LocalPart,
            match.ConfiguredSmtpCode,
            match.Enabled);
        return ValueTask.FromResult(QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Succeeded(summary));
    }
}
