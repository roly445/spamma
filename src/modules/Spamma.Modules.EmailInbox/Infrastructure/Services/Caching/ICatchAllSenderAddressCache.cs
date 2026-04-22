namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Caching;

public interface ICatchAllSenderAddressCache
{
    Task<ICatchAllSenderAddressCache.CachedSenderAddress?> GetSenderAddressAsync(
        string senderAddress,
        bool forceRefresh,
        CancellationToken cancellationToken);

    public record CachedSenderAddress(Guid SenderAddressId, string SenderAddress, IReadOnlyList<Guid> AssignedUserIds);
}
