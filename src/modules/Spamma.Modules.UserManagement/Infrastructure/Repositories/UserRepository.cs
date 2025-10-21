using Marten;
using MaybeMonad;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Domain.UserAggregate;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Infrastructure.Repositories;

internal class UserRepository(IDocumentSession session) : GenericRepository<User>(session), IUserRepository
{
    private readonly IDocumentSession session = session;

    // Add user-specific methods if needed
    public async Task<Maybe<User>> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default)
    {
        // First, query the read model to find the user ID
        var userLookup = await this.session.Query<UserLookup>()
            .FirstOrDefaultAsync(x => x.EmailAddress == emailAddress, ct);

        if (userLookup == null)
        {
            return Maybe<User>.Nothing;
        }

        // Then load the full aggregate by ID using event sourcing
        return await this.GetByIdAsync(userLookup.Id, ct);
    }
}