using Spamma.Modules.EmailInbox.Client;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

public record EmailReceived(Guid EmailId, Guid DomainId, Guid SubdomainId, string Subject, DateTime WhenSent, IReadOnlyList<EmailReceived.EmailAddress> EmailAddresses)
{
    public record EmailAddress(string Address, string Name, EmailAddressType EmailAddressType);
}