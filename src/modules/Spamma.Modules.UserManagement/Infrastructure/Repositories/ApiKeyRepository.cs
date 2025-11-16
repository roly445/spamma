using System.Security.Cryptography;
using System.Text;
using Marten;
using MaybeMonad;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Repositories;

internal class ApiKeyRepository(IDocumentSession session) : GenericRepository<ApiKey>(session), IApiKeyRepository
{
    private readonly IDocumentSession session = session;

    public async Task<Maybe<ApiKey>> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        var apiKeyLookup = await this.session.Query<ApiKeyLookup>()
            .FirstOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);

        if (apiKeyLookup == null)
        {
            return Maybe<ApiKey>.Nothing;
        }

        return await this.GetByIdAsync(apiKeyLookup.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var apiKeyLookups = await this.session.Query<ApiKeyLookup>()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var apiKeys = new List<ApiKey>();
        foreach (var lookup in apiKeyLookups)
        {
            var apiKey = await this.GetByIdAsync(lookup.Id, cancellationToken);
            if (apiKey.HasValue)
            {
                apiKeys.Add(apiKey.Value);
            }
        }

        return apiKeys;
    }

    public async Task<Maybe<ApiKey>> GetByPlainKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Maybe<ApiKey>.Nothing;
        }

        // Extract prefix (first 8 characters) and compute prefix hash for efficient lookup
        var prefix = apiKey.Substring(0, Math.Min(8, apiKey.Length));
        var prefixHash = ComputePrefixHash(prefix);

        // Query by prefix hash - should return 0-2 results in normal cases
        var apiKeyLookups = await this.session.Query<ApiKeyLookup>()
            .Where(x => x.KeyHashPrefix == prefixHash)
            .ToListAsync(cancellationToken);

        foreach (var lookup in apiKeyLookups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(lookup.KeyHash))
            {
                continue;
            }

            try
            {
                // Verify full API key against bcrypt hash
                if (BCrypt.Net.BCrypt.Verify(apiKey, lookup.KeyHash))
                {
                    var aggregate = await this.GetByIdAsync(lookup.Id, cancellationToken).ConfigureAwait(false);
                    if (aggregate.HasValue)
                    {
                        return aggregate;
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid bcrypt hash format, continue to next candidate
            }
        }

        return Maybe<ApiKey>.Nothing;
    }

    private static string ComputePrefixHash(string prefix)
    {
        using var sha256 = SHA256.Create();
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var hashBytes = sha256.ComputeHash(prefixBytes);
        return Convert.ToBase64String(hashBytes);
    }
}