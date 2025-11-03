using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domain-management/chaos-addresses/search")]
public record SearchChaosAddressesQuery(
    string? SearchTerm = null,
    Guid? SubdomainId = null,
    bool? Enabled = null,
    int PageNumber = 1,
    int PageSize = 50,
    string SortBy = "CreatedAt",
    bool SortDescending = true) : IQuery<SearchChaosAddressesQueryResult>;
