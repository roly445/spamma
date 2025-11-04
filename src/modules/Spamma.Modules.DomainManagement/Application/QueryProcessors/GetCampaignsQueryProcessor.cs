using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class GetCampaignsQueryProcessor(IDocumentSession session) : IQueryProcessor<GetCampaignsQuery, GetCampaignsQueryResult>
{
    public async Task<QueryResult<GetCampaignsQueryResult>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var query = session.Query<CampaignSummary>()
            .Where(c => c.SubdomainId == request.SubdomainId);

        var totalCount = await query.CountAsync(cancellationToken);

        var sortedQuery = request.SortDescending
            ? query.OrderByDescending(c => GetSortProperty(c, request.SortBy))
            : query.OrderBy(c => GetSortProperty(c, request.SortBy));

        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new GetCampaignsQueryResult(
            Items: items
                .Select(c => new GetCampaignsQueryResult.CampaignSummary(
                    c.CampaignId,
                    c.CampaignValue,
                    c.FirstReceivedAt,
                    c.LastReceivedAt,
                    c.TotalCaptured))
                .ToList(),
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: (int)Math.Ceiling((double)totalCount / request.PageSize));

        return QueryResult<GetCampaignsQueryResult>.Succeeded(result);
    }

    private static object GetSortProperty(CampaignSummary campaign, string sortBy) =>
        sortBy switch
        {
            "CampaignValue" => campaign.CampaignValue,
            "FirstReceivedAt" => campaign.FirstReceivedAt,
            "TotalCaptured" => campaign.TotalCaptured,
            _ => campaign.LastReceivedAt,
        };
}
