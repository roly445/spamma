using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

public record ReceivedEmailCommand(
    Guid EmailId,
    Guid DomainId,
    Guid SubdomainId,
    string Subject,
    DateTimeOffset WhenSent,
    IReadOnlyList<EmailAddress> EmailAddresses) : ICommand;