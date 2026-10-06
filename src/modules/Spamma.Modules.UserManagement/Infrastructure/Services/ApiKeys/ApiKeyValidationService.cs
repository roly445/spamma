using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;
using Spamma.Modules.UserManagement.Application.Repositories;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

internal class ApiKeyValidationService(IApiKeyRepository apiKeyRepository, IDistributedCache cache) : IApiKeyValidationService
{
    public async Task<Guid?> GetApiKeyOwnerIdAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var key = await apiKeyRepository.GetByPlainKeyAsync(apiKey, cancellationToken);
        return key.HasValue && key.Value.IsActive && key.Value.UserId != Guid.Empty ? key.Value.UserId : null;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        // A successful validation must always consult the aggregate so revocation and expiry take effect immediately.
        // Only failed lookups are cached.
        var cacheKey = $"api_key_validation:{apiKey}";
        var cachedResult = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedResult == "false")
        {
            return false;
        }

        // Use repository helper to find matching aggregate via prefix hash lookup
        // This now uses PAT pattern with O(1) prefix hash lookup and bcrypt verification
        var apiKeyAggregate = await apiKeyRepository.GetByPlainKeyAsync(apiKey, cancellationToken);
        if (!apiKeyAggregate.HasValue)
        {
            // Cache negative result for 5 minutes
            await cache.SetStringAsync(cacheKey, "false", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            }, cancellationToken);
            return false;
        }

        // Check if key is active (not revoked and not expired)
        if (!apiKeyAggregate.Value.IsActive)
        {
            await cache.SetStringAsync(cacheKey, "false", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            }, cancellationToken);
            return false;
        }

        return true;
    }
}
