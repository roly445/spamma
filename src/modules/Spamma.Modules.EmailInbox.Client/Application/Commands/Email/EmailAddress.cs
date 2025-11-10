using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

public record EmailAddress(string Address, string Name, EmailAddressType EmailAddressType);