using MaybeMonad;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;

public interface ISubdomainCache
{
    Task<Maybe<SearchSubdomainsQueryResult.SubdomainSummary>> GetSubdomainAsync(
        string domain,
        bool forceRefresh = false,
        bool cacheOnly = false,
        CancellationToken cancellationToken = default);

    Task SetSubdomainAsync(
        string domain,
        SearchSubdomainsQueryResult.SubdomainSummary subdomain,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(string domain, CancellationToken cancellationToken = default);

    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
}
