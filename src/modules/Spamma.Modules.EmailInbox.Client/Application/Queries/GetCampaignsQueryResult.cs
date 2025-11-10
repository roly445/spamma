using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetCampaignsQueryResult(
    IReadOnlyList<GetCampaignsQueryResult.CampaignSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record CampaignSummary(
        Guid CampaignId,
        Guid DomainId,
        Guid SubdomainId,
        string CampaignValue,
        DateTimeOffset FirstReceivedAt,
        DateTimeOffset LastReceivedAt,
        int TotalCaptured);
}
