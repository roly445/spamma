using BluQube.Attributes;
using BluQube.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domains/search")]
public record SearchDomainsQuery(string? SearchTerm, DomainStatus? Status, bool? IsVerified, int Page, int PageSize, string SortBy, bool SortDescending) : IQuery<SearchDomainsQueryResult>;