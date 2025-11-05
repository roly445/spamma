using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands;

public record ReceivedEmailCommand(Guid EmailId, Guid DomainId, Guid SubdomainId, string Subject, DateTime WhenSent, IReadOnlyList<ReceivedEmailCommand.EmailAddress> EmailAddresses) : ICommand
{
    public record EmailAddress(string Address, string Name, EmailAddressType EmailAddressType);
}