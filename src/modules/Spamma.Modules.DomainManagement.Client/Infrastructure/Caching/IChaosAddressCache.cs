using MaybeMonad;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;

/// <summary>
/// Cache wrapper for chaos address lookups by subdomain ID and local part.
/// Provides caching layer with TTL and invalidation support via CAP integration events.
/// </summary>
public interface IChaosAddressCache
{
    /// <summary>
    /// Get chaos address by subdomain ID and local part from cache or database.
    /// </summary>
    /// <param name="subdomainId">Subdomain aggregate ID</param>
    /// <param name="localPart">Email local part (before @domain)</param>
    /// <param name="forceRefresh">Force cache invalidation and fetch from database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chaos address if found and enabled, Nothing otherwise</returns>
    Task<Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>> GetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly set chaos address in cache with TTL.
    /// </summary>
    /// <param name="subdomainId">Subdomain aggregate ID</param>
    /// <param name="localPart">Email local part</param>
    /// <param name="chaosAddress">Chaos address data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        GetChaosAddressBySubdomainAndLocalPartQueryResult chaosAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cache entry for specific chaos address.
    /// Called by CAP event handlers when chaos address is updated/deleted.
    /// </summary>
    /// <param name="subdomainId">Subdomain aggregate ID</param>
    /// <param name="localPart">Email local part</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateAsync(Guid subdomainId, string localPart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate all chaos address entries for a subdomain.
    /// Called when subdomain is deleted or all chaos addresses are cleared.
    /// </summary>
    /// <param name="subdomainId">Subdomain aggregate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateBySubdomainAsync(Guid subdomainId, CancellationToken cancellationToken = default);
}
