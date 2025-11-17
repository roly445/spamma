using MaybeMonad;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

namespace Spamma.Modules.UserManagement.Application.Repositories;

internal interface IPasskeyRepository : IRepository<Passkey>
{
    Task<Maybe<Passkey>> GetByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default);
}