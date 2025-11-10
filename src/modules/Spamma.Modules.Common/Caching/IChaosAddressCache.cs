using MaybeMonad;
using SmtpServer.Protocol;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.Common.Caching;

public interface IChaosAddressCache
{
    Task<Maybe<CachedChaosAddress>> GetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    Task SetChaosAddressAsync(
        Guid subdomainId,
        string localPart,
        CachedChaosAddress chaosAddress,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid subdomainId, string localPart, CancellationToken cancellationToken = default);

    Task InvalidateBySubdomainAsync(Guid subdomainId, CancellationToken cancellationToken = default);

    public record CachedChaosAddress(
        Guid ChaosAddressId,
        Guid DomainId,
        Guid SubdomainId,
        SmtpResponseCode ConfiguredSmtpCode);
}