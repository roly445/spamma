using Marten;
using MaybeMonad;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Repositories;

public class PasskeyRepository(IDocumentSession session) : GenericRepository<Passkey>(session), IPasskeyRepository
{
    private readonly IDocumentSession _session = session;

    public async Task<Maybe<Passkey>> GetByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default)
    {
        // Query the PasskeyLookup read model to find the passkey ID by credential ID
        var lookup = await this._session
            .Query<PasskeyLookup>()
            .FirstOrDefaultAsync(p => p.CredentialId == credentialId && !p.IsRevoked, cancellationToken);

        if (lookup == null)
        {
            return Maybe<Passkey>.Nothing;
        }

        // Load the aggregate by its ID using the base class method (event sourcing)
        return await this.GetByIdAsync(lookup.Id, cancellationToken);
    }
}