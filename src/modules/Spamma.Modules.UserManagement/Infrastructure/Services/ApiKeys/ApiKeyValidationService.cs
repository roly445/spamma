using System.Security.Cryptography;
using Spamma.Modules.UserManagement.Application.Repositories;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

internal class ApiKeyValidationService(IApiKeyRepository apiKeyRepository) : IApiKeyValidationService
{
    private readonly IApiKeyRepository apiKeyRepository = apiKeyRepository;

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        // Generate hash of the provided key to find matching records
        var (keyHash, _) = GenerateKeyHash(apiKey);

        var apiKeyAggregate = await this.apiKeyRepository.GetByKeyHashAsync(keyHash, cancellationToken);
        if (!apiKeyAggregate.HasValue)
        {
            return false;
        }

        // Verify the key by re-hashing with the stored salt
        var expectedHash = GenerateKeyHash(apiKey, apiKeyAggregate.Value.Salt);
        return expectedHash.KeyHash == apiKeyAggregate.Value.KeyHash && !apiKeyAggregate.Value.IsRevoked;
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