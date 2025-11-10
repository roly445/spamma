using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

public class GetCampaignsQueryProcessor(IDocumentSession session) : IQueryProcessor<GetCampaignsQuery, GetCampaignsQueryResult>
{
    public async Task<QueryResult<GetCampaignsQueryResult>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var query = session.Query<CampaignSummary>()
            .Where(c => c.SubdomainId == request.SubdomainId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting based on request
        IQueryable<CampaignSummary> sortedQuery = request.SortBy switch
        {
            "CampaignValue" => request.SortDescending
                ? query.OrderByDescending(c => c.CampaignValue)
                : query.OrderBy(c => c.CampaignValue),
            "FirstReceivedAt" => request.SortDescending
                ? query.OrderByDescending(c => c.FirstReceivedAt)
                : query.OrderBy(c => c.FirstReceivedAt),
            "TotalCaptured" => request.SortDescending
                ? query.OrderByDescending(c => c.TotalCaptured)
                : query.OrderBy(c => c.TotalCaptured),
            _ => request.SortDescending
                ? query.OrderByDescending(c => c.LastReceivedAt)
                : query.OrderBy(c => c.LastReceivedAt),
        };

        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new GetCampaignsQueryResult(
            Items: items
                .Select(c => new GetCampaignsQueryResult.CampaignSummary(
                    c.CampaignId,
                    c.DomainId,
                    c.SubdomainId,
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
}
