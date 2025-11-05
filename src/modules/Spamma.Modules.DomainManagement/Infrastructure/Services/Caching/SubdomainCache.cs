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

/// <summary>
/// Redis-backed cache for subdomain lookups by domain name.
/// Uses direct IConnectionMultiplexer for full Redis capabilities including pattern-based invalidation.
/// Cache keys: subdomain:{domain.ToLower()}
/// TTL: 1 hour (or until invalidated by CAP event)
/// </summary>
public class SubdomainCache(
    IConnectionMultiplexer redisMultiplexer,
    IQuerier querier,
    ILogger<SubdomainCache> logger, IInternalQueryStore internalQueryStore) : ISubdomainCache
{
    private const string CacheKeyPrefix = "subdomain:";
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

        // Cache miss or forceRefresh: query database
        logger.LogDebug("Cache MISS for subdomain: {Domain}", domain);
        var query = new SearchSubdomainsQuery(
            SearchTerm: domain.ToLowerInvariant(),
            ParentDomainId: null,
            Status: SubdomainStatus.Active,
            Page: 1,
            PageSize: 1,
            SortBy: "domainname",
            SortDescending: false);
        internalQueryStore.AddReferenceForObject(query);
        var result = await querier.Send(query, cancellationToken);

        if (result.Status != QueryResultStatus.Succeeded || result.Data.TotalCount == 0)
        {
            return Maybe<SearchSubdomainsQueryResult.SubdomainSummary>.Nothing;
        }

        var subdomain = result.Data.Items[0];

        // Cache the result
        try
        {
            await this.SetSubdomainAsync(domain, subdomain, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache subdomain for {Domain}", domain);

            // Continue despite cache failure - fallback to uncached operation
        }

        return Maybe.From(subdomain);
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
