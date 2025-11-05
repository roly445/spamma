using MaybeMonad;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;

/// <summary>
/// Cache wrapper for subdomain lookups by domain name.
/// Provides caching layer with TTL and invalidation support via CAP integration events.
/// </summary>
public interface ISubdomainCache
{
    /// <summary>
    /// Get subdomain by domain name from cache or database.
    /// </summary>
    /// <param name="domain">Domain name (e.g., "example.com")</param>
    /// <param name="forceRefresh">Force cache invalidation and fetch from database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subdomain summary if found and active, Nothing otherwise</returns>
    Task<Maybe<SearchSubdomainsQueryResult.SubdomainSummary>> GetSubdomainAsync(
        string domain,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly set subdomain in cache with TTL.
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <param name="subdomain">Subdomain summary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetSubdomainAsync(
        string domain,
        SearchSubdomainsQueryResult.SubdomainSummary subdomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cache entry for domain.
    /// Called by CAP event handlers when subdomain is updated/deleted.
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cache entries for multiple domains by pattern.
    /// </summary>
    /// <param name="pattern">Key pattern (e.g., "subdomain:*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
}
