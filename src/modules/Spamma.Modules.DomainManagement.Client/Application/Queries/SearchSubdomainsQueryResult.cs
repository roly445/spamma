using BluQube.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record SearchSubdomainsQueryResult(
    IReadOnlyList<SearchSubdomainsQueryResult.SubdomainSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record SubdomainSummary(
        Guid Id,
        Guid ParentDomainId,
        string SubdomainName,
        string ParentDomainName,
        string FullDomainName,
        SubdomainStatus Status,
        DateTime CreatedAt,
        int ChaosMonkeyRuleCount,
        int ActiveCampaignCount,
        string? Description);
}