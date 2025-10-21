using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AccountSuspended(AccountSuspensionReason Reason, string? Notes, DateTime WhenSuspended, Guid SecurityStamp);