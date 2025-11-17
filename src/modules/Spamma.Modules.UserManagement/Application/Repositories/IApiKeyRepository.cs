using MaybeMonad;
using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.UserManagement.Application.Repositories;

internal interface IApiKeyRepository : IRepository<Domain.ApiKeys.ApiKey>
{
    Task<Maybe<Domain.ApiKeys.ApiKey>> GetByPlainKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}