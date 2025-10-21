using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/subdomains/moderators/search")]
public record SearchSubdomainModeratorsQuery(
    Guid SubdomainId,
    string? SearchTerm = null,
    string SortBy = "AssignedAt",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 10) : IQuery<SearchSubdomainModeratorsQueryResult>;