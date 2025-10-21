using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record SearchDomainModeratorsQueryResult(
    IReadOnlyList<SearchDomainModeratorsQueryResult.DomainModeratorSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record DomainModeratorSummary(
        Guid UserId,
        string DisplayName,
        string Email,
        DateTime AssignedAt);
}