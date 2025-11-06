using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record SearchEmailsQueryResult(
    IReadOnlyList<SearchEmailsQueryResult.EmailSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record EmailSummary(Guid EmailId, string Subject, string PrimaryToAddress, DateTime ReceivedAt, bool IsFavorite, Guid? CampaignId);
}