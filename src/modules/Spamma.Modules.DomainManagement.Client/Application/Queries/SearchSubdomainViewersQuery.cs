using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/subdomains/viewers/search")]
public record SearchSubdomainViewersQuery(
    Guid SubdomainId,
    string? SearchTerm = null,
    string SortBy = "AssignedAt",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 10) : IQuery<SearchSubdomainViewersQueryResult>;