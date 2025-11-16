using Microsoft.Extensions.Caching.Distributed;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

internal class ApiKeyRateLimiter(IDistributedCache cache) : IApiKeyRateLimiter
{
    // Rate limits: 1000 requests per hour per API key
    private const int MaxRequestsPerHour = 1000;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromHours(1);
    private readonly IDistributedCache cache = cache;

    public async Task<bool> IsWithinRateLimitAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetRateLimitCacheKey(apiKey);
        var currentCount = await this.GetCurrentRequestCountAsync(cacheKey, cancellationToken);

        return currentCount < MaxRequestsPerHour;
    }

    public async Task RecordRequestAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetRateLimitCacheKey(apiKey);
        var currentCount = await this.GetCurrentRequestCountAsync(cacheKey, cancellationToken);

        // Increment and store the new count
        await this.cache.SetStringAsync(
            cacheKey,
            (currentCount + 1).ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = WindowDuration,
            },
            cancellationToken);
    }

    private static string GetRateLimitCacheKey(string apiKey)
    {
        // Use a hash of the API key for the cache key to avoid storing the key itself
        var keyHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(apiKey)));
        return $"api_key_rate_limit:{keyHash}";
    }

    private async Task<int> GetCurrentRequestCountAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var cachedValue = await this.cache.GetStringAsync(cacheKey, cancellationToken);
        return int.TryParse(cachedValue, out var count) ? count : 0;
    }
}