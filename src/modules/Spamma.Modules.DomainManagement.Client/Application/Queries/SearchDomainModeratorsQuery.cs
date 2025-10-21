using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domains/moderators/search")]
public record SearchDomainModeratorsQuery(
    Guid DomainId,
    string? SearchTerm = null,
    string SortBy = "AssignedAt",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 10) : IQuery<SearchDomainModeratorsQueryResult>;