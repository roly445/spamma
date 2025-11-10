using MaybeMonad;

namespace Spamma.Modules.Common.Caching;

public interface ISubdomainCache
{
    Task<Maybe<CachedSubdomain>> GetSubdomainAsync(
        string domain,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    Task SetSubdomainAsync(
        string domain,
        CachedSubdomain subdomain,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(string domain, CancellationToken cancellationToken = default);

    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);

    public record CachedSubdomain(Guid SubdomainId, Guid DomainId);
}