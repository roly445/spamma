using System.Text.Json;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using StackExchange.Redis;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Caching;

public class CatchAllSenderAddressCache(
    IConnectionMultiplexer redisMultiplexer,
    IQuerySession querySession,
    ILogger<CatchAllSenderAddressCache> logger) : ICatchAllSenderAddressCache
{
    private const string CacheKeyPrefix = "catchall-sender:";
    private const string NullSentinel = "null";

    private readonly IDatabase _redis = redisMultiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _nullTtl = TimeSpan.FromMinutes(1);

    public async Task<ICatchAllSenderAddressCache.CachedSenderAddress?> GetSenderAddressAsync(
        string senderAddress,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            return null;
        }

        var normalizedAddress = senderAddress.ToLowerInvariant();
        var cacheKey = CreateCacheKey(normalizedAddress);

        if (!forceRefresh)
        {
            var cachedValue = await this._redis.StringGetAsync(cacheKey);
            if (cachedValue.HasValue)
            {
                var raw = cachedValue.ToString();
                if (raw == NullSentinel)
                {
                    logger.LogDebug("Cache HIT (null sentinel) for sender address: {SenderAddress}", normalizedAddress);
                    return null;
                }

                try
                {
                    var cached = JsonSerializer.Deserialize<ICatchAllSenderAddressCache.CachedSenderAddress>(raw);
                    if (cached != null)
                    {
                        logger.LogDebug("Cache HIT for sender address: {SenderAddress}", normalizedAddress);
                        return cached;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached sender address for {SenderAddress}", normalizedAddress);
                    await this._redis.KeyDeleteAsync(cacheKey);
                }
            }
        }

        logger.LogDebug("Cache MISS for sender address: {SenderAddress}", normalizedAddress);

        var lookup = await querySession.Query<CatchAllSenderAddressLookup>()
            .FirstOrDefaultAsync(x => x.SenderAddress == normalizedAddress && !x.IsRemoved, cancellationToken);

        if (lookup == null)
        {
            try
            {
                await this._redis.StringSetAsync(cacheKey, NullSentinel, this._nullTtl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cache null sentinel for sender address {SenderAddress}", normalizedAddress);
            }

            return null;
        }

        var result = new ICatchAllSenderAddressCache.CachedSenderAddress(
            lookup.Id,
            lookup.SenderAddress,
            lookup.AssignedUserIds);

        try
        {
            var serialized = JsonSerializer.SerializeToUtf8Bytes(result);
            await this._redis.StringSetAsync(cacheKey, serialized, this._ttl);
            logger.LogDebug("Cached sender address: {SenderAddress} with TTL {TTL}", normalizedAddress, this._ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache sender address {SenderAddress}", normalizedAddress);
        }

        return result;
    }

    private static string CreateCacheKey(string normalizedAddress) =>
        $"{CacheKeyPrefix}{normalizedAddress}";
}
