using System.Text.Json;
using BluQube.Constants;
using BluQube.Queries;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;
using StackExchange.Redis;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services.Caching;

public class ChaosAddressCache(
    IConnectionMultiplexer redisMultiplexer,
    IQuerier querier,
    ILogger<ChaosAddressCache> logger, IInternalQueryStore internalQueryStore) : IChaosAddressCache
{
    private const string CacheKeyPrefix = "chaos:";
    private readonly IDatabase _redis = redisMultiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(1);

    public async Task<Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>> GetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (subdomainId == Guid.Empty || string.IsNullOrWhiteSpace(localPart))
        {
            return Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing;
        }

        var cacheKey = CreateCacheKey(subdomainId, localPart);

        if (!forceRefresh)
        {
            var cachedValue = this._redis.StringGet(cacheKey);
            if (cachedValue.HasValue)
            {
                try
                {
                    var cachedChaosAddress = JsonSerializer.Deserialize<GetChaosAddressBySubdomainAndLocalPartQueryResult?>(cachedValue.ToString());
                    if (cachedChaosAddress != null)
                    {
                        logger.LogDebug("Cache HIT for chaos address: {SubdomainId}:{LocalPart}", subdomainId, localPart);
                        return Maybe.From(cachedChaosAddress);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached chaos address for {SubdomainId}:{LocalPart}", subdomainId, localPart);
                    await this.InvalidateAsync(subdomainId, localPart, cancellationToken);
                }
            }
        }

        logger.LogDebug("Cache MISS for chaos address: {SubdomainId}:{LocalPart}", subdomainId, localPart);
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(subdomainId, localPart);
        internalQueryStore.StoreQueryRef(query);
        var result = await querier.Send(query, cancellationToken);

        if (result.Status != QueryResultStatus.Succeeded)
        {
            return Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>.Nothing;
        }

        var chaosAddress = result.Data;

        try
        {
            await this.SetChaosAddressAsync(subdomainId, localPart, chaosAddress, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache chaos address for {SubdomainId}:{LocalPart}", subdomainId, localPart);
        }

        return Maybe.From(chaosAddress);
    }

    public async Task SetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        GetChaosAddressBySubdomainAndLocalPartQueryResult chaosAddress,
        CancellationToken cancellationToken = default)
    {
        if (subdomainId == Guid.Empty || string.IsNullOrWhiteSpace(localPart))
        {
            return;
        }

        var cacheKey = CreateCacheKey(subdomainId, localPart);

        try
        {
            var serialized = JsonSerializer.SerializeToUtf8Bytes(chaosAddress);
            this._redis.StringSet(cacheKey, serialized, this._ttl);
            logger.LogDebug("Cached chaos address: {SubdomainId}:{LocalPart} with TTL {TTL}", subdomainId, localPart, this._ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set cache for chaos address {SubdomainId}:{LocalPart}", subdomainId, localPart);
        }

        await Task.CompletedTask;
    }

    public async Task InvalidateAsync(Guid subdomainId, string localPart, CancellationToken cancellationToken = default)
    {
        if (subdomainId == Guid.Empty || string.IsNullOrWhiteSpace(localPart))
        {
            return;
        }

        var cacheKey = CreateCacheKey(subdomainId, localPart);

        try
        {
            this._redis.KeyDelete(cacheKey);
            logger.LogInformation("Invalidated cache for chaos address: {SubdomainId}:{LocalPart}", subdomainId, localPart);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache for chaos address {SubdomainId}:{LocalPart}", subdomainId, localPart);
        }

        await Task.CompletedTask;
    }

    public async Task InvalidateBySubdomainAsync(Guid subdomainId, CancellationToken cancellationToken = default)
    {
        if (subdomainId == Guid.Empty)
        {
            return;
        }

        try
        {
            var pattern = $"{CacheKeyPrefix}{subdomainId}:*";
            var server = redisMultiplexer.GetServer(redisMultiplexer.GetEndPoints()[0]);
            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                this._redis.KeyDelete(keys);
                logger.LogInformation("Invalidated {Count} cache entries for subdomain: {SubdomainId}", keys.Length, subdomainId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache entries for subdomain {SubdomainId}", subdomainId);
        }

        await Task.CompletedTask;
    }

    private static string CreateCacheKey(Guid subdomainId, string localPart) =>
        $"{CacheKeyPrefix}{subdomainId}:{localPart.ToLowerInvariant()}";
}
