using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

public record DomainSuspended(DomainSuspensionReason Reason, string? Notes, DateTime WhenSuspended);