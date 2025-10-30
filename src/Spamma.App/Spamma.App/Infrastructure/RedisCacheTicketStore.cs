using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Spamma.App.Infrastructure;

public class RedisCacheTicketStore(RedisCacheOptions options) : ITicketStore
{
    private readonly IDistributedCache _cache = new RedisCache(options);

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var guid = Guid.NewGuid();
        var rawKey = "AuthSessionStore-" + guid.ToString();
        var key = RedisKeys.WithPrefix(rawKey);
        await this.RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var cacheEntryOptions = new DistributedCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;
        if (expiresUtc.HasValue)
        {
            cacheEntryOptions.SetAbsoluteExpiration(expiresUtc.Value);
        }

        byte[] val = SerializeToBytes(ticket);
        this._cache.Set(key, val, cacheEntryOptions);
        return Task.FromResult(0);
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = this._cache.Get(key);
        var ticket = DeserializeFromBytes(bytes);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        this._cache.Remove(key);
        return Task.FromResult(0);
    }

    private static byte[] SerializeToBytes(AuthenticationTicket source)
    {
        return TicketSerializer.Default.Serialize(source);
    }

    private static AuthenticationTicket? DeserializeFromBytes(byte[]? source)
    {
        return source == null ? null : TicketSerializer.Default.Deserialize(source);
    }
}