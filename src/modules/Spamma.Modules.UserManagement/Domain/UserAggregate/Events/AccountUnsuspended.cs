namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AccountUnsuspended(DateTime SuspendedAt, Guid SecurityStamp);