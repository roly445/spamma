using Spamma.Modules.EmailInbox.Client;

namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public record EmailAddress(string Address, string Name, EmailAddressType EmailAddressType);