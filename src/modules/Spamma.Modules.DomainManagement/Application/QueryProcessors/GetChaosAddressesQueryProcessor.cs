using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class GetChaosAddressesQueryProcessor(IDocumentSession session) : IQueryProcessor<GetChaosAddressesQuery, GetChaosAddressesQueryResult>
{
    public async Task<QueryResult<GetChaosAddressesQueryResult>> Handle(GetChaosAddressesQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<ChaosAddressLookup>();

        var filteredQuery = request.SubdomainId.HasValue
            ? baseQuery.Where(x => x.SubdomainId == request.SubdomainId.Value)
            : baseQuery;

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var summaries = items.Select(x => new ChaosAddressSummary(
            x.Id,
            x.DomainId,
            x.SubdomainId,
            x.LocalPart,
            x.ConfiguredSmtpCode,
            x.Enabled,
            x.TotalReceived,
            x.LastReceivedAt,
            x.CreatedAt)).ToList();

        var result = new GetChaosAddressesQueryResult(summaries, totalCount, request.PageNumber, request.PageSize);
        return QueryResult<GetChaosAddressesQueryResult>.Succeeded(result);
    }
}