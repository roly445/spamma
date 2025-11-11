using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;
using Spamma.Modules.UserManagement.Application.Repositories;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

internal class ApiKeyValidationService(IApiKeyRepository apiKeyRepository, IDistributedCache cache) : IApiKeyValidationService
{
    private readonly IApiKeyRepository apiKeyRepository = apiKeyRepository;
    private readonly IDistributedCache cache = cache;

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        // Check cache first
        var cacheKey = $"api_key_validation:{apiKey}";
        var cachedResult = await this.cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return bool.Parse(cachedResult);
        }

        // Generate hash of the provided key to find matching records
        var (keyHash, _) = GenerateKeyHash(apiKey);

        var apiKeyAggregate = await this.apiKeyRepository.GetByKeyHashAsync(keyHash, cancellationToken);
        if (!apiKeyAggregate.HasValue)
        {
            // Cache negative result for 5 minutes
            await this.cache.SetStringAsync(cacheKey, "false", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            }, cancellationToken);
            return false;
        }

        // Check if the key is expired
        if (apiKeyAggregate.Value.IsExpired)
        {
            // Cache negative result for 5 minutes
            await this.cache.SetStringAsync(cacheKey, "false", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            }, cancellationToken);
            return false;
        }

        // Verify the key by re-hashing with the stored salt
        var expectedHash = GenerateKeyHash(apiKey, apiKeyAggregate.Value.Salt);
        var isValid = expectedHash.KeyHash == apiKeyAggregate.Value.KeyHash && !apiKeyAggregate.Value.IsRevoked;

        // Cache positive result for 30 minutes, negative for 5 minutes
        await this.cache.SetStringAsync(cacheKey, isValid.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = isValid ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(5),
        }, cancellationToken);

        return isValid;
    }

    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        RandomNumberGenerator.Fill(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static (string KeyHash, string Salt) GenerateKeyHash(string apiKey, string? salt = null)
    {
        salt ??= GenerateSalt();

        using var hmac = new HMACSHA256(Convert.FromBase64String(salt));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(apiKey));
        var keyHash = Convert.ToBase64String(hashBytes);

        return (keyHash, salt);
    }
}