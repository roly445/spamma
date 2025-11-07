using MaybeMonad;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;

public interface IChaosAddressCache
{
    Task<Maybe<GetChaosAddressBySubdomainAndLocalPartQueryResult>> GetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    Task SetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        GetChaosAddressBySubdomainAndLocalPartQueryResult chaosAddress,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid subdomainId, string localPart, CancellationToken cancellationToken = default);

    Task InvalidateBySubdomainAsync(Guid subdomainId, CancellationToken cancellationToken = default);
}
