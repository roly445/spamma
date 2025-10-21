using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record SearchSubdomainModeratorsQueryResult(
    IReadOnlyList<SearchSubdomainModeratorsQueryResult.SubdomainModeratorSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record SubdomainModeratorSummary(
        Guid UserId,
        string DisplayName,
        string Email,
        DateTime AssignedAt);
}