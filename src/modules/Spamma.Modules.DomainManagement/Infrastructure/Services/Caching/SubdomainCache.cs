using System.Text.Json;
using BluQube.Constants;
using BluQube.Queries;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using StackExchange.Redis;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services.Caching;

public class SubdomainCache(
    IConnectionMultiplexer redisMultiplexer,
    IQuerier querier,
    ILogger<SubdomainCache> logger, IInternalQueryStore internalQueryStore) : ISubdomainCache
{
    private const string CacheKeyPrefix = "subdomain:";

    private readonly IDatabase _redis = redisMultiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(1);

    public async Task<Maybe<ISubdomainCache.CachedSubdomain>> GetSubdomainAsync(
        string domain,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return Maybe<ISubdomainCache.CachedSubdomain>.Nothing;
        }

        var cacheKey = CreateCacheKey(domain);

        if (!forceRefresh)
        {
            var cachedValue = await this._redis.StringGetAsync(cacheKey);
            if (cachedValue.HasValue)
            {
                try
                {
                    var cachedSubdomain = JsonSerializer.Deserialize<ISubdomainCache.CachedSubdomain>(cachedValue.ToString());
                    if (cachedSubdomain != null)
                    {
                        return Maybe.From(cachedSubdomain);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached subdomain for {Domain}", domain);
                    await this.InvalidateAsync(domain, cancellationToken);
                }
            }
        }

        logger.LogDebug("Cache MISS for subdomain: {Domain}", domain);

        // Query the database (no stampede protection - caller should deduplicate requests)
        var query = new SearchSubdomainsQuery(
            SearchTerm: domain.ToLowerInvariant(),
            ParentDomainId: null,
            Status: null,
            Page: 1,
            PageSize: 1,
            SortBy: "domainname",
            SortDescending: false);
        internalQueryStore.StoreQueryRef(query);
        var result = await querier.Send(query, cancellationToken);

        if (result.Status != QueryResultStatus.Succeeded || result.Data.TotalCount == 0)
        {
            return Maybe<ISubdomainCache.CachedSubdomain>.Nothing;
        }

        var subdomain = result.Data.Items[0];
        if (subdomain.Status == SubdomainStatus.Suspended)
        {
            return Maybe<ISubdomainCache.CachedSubdomain>.Nothing;
        }

        var subdomainForCaching = new ISubdomainCache.CachedSubdomain(result.Data.Items[0].SubdomainId, result.Data.Items[0].ParentDomainId);

        try
        {
            await this.SetSubdomainAsync(domain, subdomainForCaching, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache subdomain for {Domain}", domain);
        }

        return Maybe.From(subdomainForCaching);
    }

    public async Task SetSubdomainAsync(
        string domain,
        ISubdomainCache.CachedSubdomain subdomain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        var cacheKey = CreateCacheKey(domain);

        try
        {
            var serialized = JsonSerializer.SerializeToUtf8Bytes(subdomain);
            await this._redis.StringSetAsync(cacheKey, serialized, this._ttl);
            logger.LogDebug("Cached subdomain: {Domain} with TTL {TTL}", domain, this._ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set cache for subdomain {Domain}", domain);
        }

        await Task.CompletedTask;
    }

    public async Task InvalidateAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        var cacheKey = CreateCacheKey(domain);

        try
        {
            this._redis.KeyDelete(cacheKey);
            logger.LogInformation("Invalidated cache for subdomain: {Domain}", domain);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache for subdomain {Domain}", domain);
        }

        await Task.CompletedTask;
    }

    public async Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = redisMultiplexer.GetServer(redisMultiplexer.GetEndPoints()[0]);
            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                this._redis.KeyDelete(keys);
                logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate cache entries with pattern {Pattern}", pattern);
        }

        await Task.CompletedTask;
    }

    private static string CreateCacheKey(string domain) => $"{CacheKeyPrefix}{domain.ToLowerInvariant()}";
}