namespace Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;

public record CatchAllSenderAddressAdded(Guid AddressId, string SenderAddress, DateTimeOffset AddedAt);
