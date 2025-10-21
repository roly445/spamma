namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AccountUnsuspended(DateTime WhenSuspended, Guid SecurityStamp);