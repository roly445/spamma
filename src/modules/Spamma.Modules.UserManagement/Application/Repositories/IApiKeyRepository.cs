using MaybeMonad;
using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.UserManagement.Application.Repositories;

internal interface IApiKeyRepository : IRepository<Domain.ApiKeys.ApiKey>
{
    Task<Maybe<Domain.ApiKeys.ApiKey>> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.ApiKeys.ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Maybe<Domain.ApiKeys.ApiKey>> GetByPlainKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}