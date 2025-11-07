using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record DetailsChanged(string EmailAddress, string Name, SystemRole SystemRole);