using BluQube.Attributes;
using BluQube.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/subdomains/search")]
public record SearchSubdomainsQuery(string? SearchTerm, Guid? ParentDomainId, SubdomainStatus? Status, int Page, int PageSize, string SortBy, bool SortDescending) : IQuery<SearchSubdomainsQueryResult>;