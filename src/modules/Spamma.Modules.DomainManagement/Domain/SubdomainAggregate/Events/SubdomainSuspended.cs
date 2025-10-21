using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record SubdomainSuspended(SubdomainSuspensionReason Reason, string? Notes, DateTime WhenSuspended);