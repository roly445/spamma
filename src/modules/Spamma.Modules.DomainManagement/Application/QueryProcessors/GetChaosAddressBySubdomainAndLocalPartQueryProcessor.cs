using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class GetChaosAddressBySubdomainAndLocalPartQueryProcessor(IDocumentSession session) : IQueryProcessor<GetChaosAddressBySubdomainAndLocalPartQuery, GetChaosAddressBySubdomainAndLocalPartQueryResult>
{
    public async ValueTask<QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>> Handle(GetChaosAddressBySubdomainAndLocalPartQuery request, CancellationToken cancellationToken)
    {
        var match = await session.Query<ChaosAddressLookup>()
            .FirstOrDefaultAsync(x => x.SubdomainId == request.SubdomainId && x.LocalPart.Equals(request.LocalPart, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (match == null)
        {
            return QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Failed();
        }

        var summary = new GetChaosAddressBySubdomainAndLocalPartQueryResult(
            match.Id,
            match.SubdomainId,
            match.DomainId,
            match.LocalPart,
            match.ConfiguredSmtpCode,
            match.Enabled,
            match.ReportsSpam);
        return QueryResult<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Succeeded(summary);
    }
}
