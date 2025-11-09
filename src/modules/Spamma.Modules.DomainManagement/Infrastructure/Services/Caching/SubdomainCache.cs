using System.Text.Json;
using BluQube.Constants;
using BluQube.Queries;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;
using StackExchange.Redis;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services.Caching;

public class SubdomainCache(
    IConnectionMultiplexer redisMultiplexer,
    IQuerier querier,
    ILogger<SubdomainCache> logger, IInternalQueryStore internalQueryStore) : ISubdomainCache
{
    private const string CacheKeyPrefix = "subdomain:";

    // Prevents cache stampede: Only one thread queries DB per domain at a time
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private readonly IDatabase _redis = redisMultiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(1);

    public async Task<Maybe<SearchSubdomainsQueryResult.SubdomainSummary>> GetSubdomainAsync(
        string domain,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return Maybe<SearchSubdomainsQueryResult.SubdomainSummary>.Nothing;
        }

        var cacheKey = CreateCacheKey(domain);

        if (!forceRefresh)
        {
            var cachedValue = this._redis.StringGet(cacheKey);
            if (cachedValue.HasValue)
            {
                try
                {
                    var cachedSubdomain = JsonSerializer.Deserialize<SearchSubdomainsQueryResult.SubdomainSummary?>(cachedValue.ToString());
                    if (cachedSubdomain != null)
                    {
                        logger.LogDebug("Cache HIT for subdomain: {Domain}", domain);
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

        // STAMPEDE PROTECTION: Only one thread queries DB per domain
        var domainLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await domainLock.WaitAsync(cancellationToken);

        try
        {
            // Double-check cache after acquiring lock (another thread may have populated it)
            var cachedValueAfterLock = this._redis.StringGet(cacheKey);
            if (cachedValueAfterLock.HasValue)
            {
                try
                {
                    var cachedSubdomain = JsonSerializer.Deserialize<SearchSubdomainsQueryResult.SubdomainSummary?>(cachedValueAfterLock.ToString());
                    if (cachedSubdomain != null)
                    {
                        logger.LogDebug("Cache HIT after lock for subdomain: {Domain}", domain);
                        return Maybe.From(cachedSubdomain);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached subdomain after lock for {Domain}", domain);
                }
            }

            // Still a miss - query the database
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
                return Maybe<SearchSubdomainsQueryResult.SubdomainSummary>.Nothing;
            }

            var subdomain = result.Data.Items[0];
            if (subdomain.Status == SubdomainStatus.Suspended)
            {
                return Maybe<SearchSubdomainsQueryResult.SubdomainSummary>.Nothing;
            }

            try
            {
                await this.SetSubdomainAsync(domain, subdomain, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cache subdomain for {Domain}", domain);
            }

            return Maybe.From(subdomain);
        }
        finally
        {
            domainLock.Release();
        }
    }

    public async Task SetSubdomainAsync(
        string domain,
        SearchSubdomainsQueryResult.SubdomainSummary subdomain,
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
            this._redis.StringSet(cacheKey, serialized, this._ttl);
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
