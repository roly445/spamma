using BluQube.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record SearchDomainsQueryResult(
    IReadOnlyList<SearchDomainsQueryResult.DomainSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record DomainSummary(
        Guid DomainId,
        string DomainName,
        DomainStatus Status,
        bool IsVerified,
        DateTime CreatedAt,
        DateTime? VerifiedAt,
        int SubdomainCount,
        int AssignedUserCount,
        string? PrimaryContact,
        string? Description,
        string? VerificationToken);
}