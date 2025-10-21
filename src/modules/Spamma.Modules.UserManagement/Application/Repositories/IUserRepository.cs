using MaybeMonad;
using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.UserManagement.Application.Repositories;

internal interface IUserRepository : IRepository<Domain.UserAggregate.User>
{
    Task<Maybe<Domain.UserAggregate.User>> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default);
}