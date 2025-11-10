using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

public record CampaignEmailReceivedCommand(
    Guid EmailId,
    Guid DomainId,
    Guid SubdomainId,
    string Subject,
    DateTimeOffset WhenSent,
    Guid CampaignId,
    IReadOnlyList<EmailAddress> EmailAddresses) : ICommand;