namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

public record ChaosAddressReceivedV2(DateTimeOffset ReceivedAt, Guid AttemptId);
