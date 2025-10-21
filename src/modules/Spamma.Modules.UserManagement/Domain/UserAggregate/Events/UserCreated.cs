using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record UserCreated(Guid UserId, string Name, string EmailAddress, Guid SecurityStamp, DateTime WhenCreated, SystemRole SystemRole);