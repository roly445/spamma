using Marten;
using MaybeMonad;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Repositories.ApiKeys;

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
}