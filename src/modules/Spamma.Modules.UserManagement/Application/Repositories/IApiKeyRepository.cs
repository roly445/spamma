using MaybeMonad;
using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.UserManagement.Application.Repositories;

public interface IApiKeyRepository : IRepository<Domain.ApiKeys.ApiKey>
{
    Task<Maybe<Domain.ApiKeys.ApiKey>> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.ApiKeys.ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}