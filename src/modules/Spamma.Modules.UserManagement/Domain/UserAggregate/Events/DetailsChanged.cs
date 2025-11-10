using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record DetailsChanged(string EmailAddress, string Name, SystemRole SystemRole);