using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

public partial class Email : AggregateRoot
{
    private readonly List<EmailAddress> _emailAddresses = new();

    private Email()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid DomainId { get; private set; }

    internal Guid SubdomainId { get; private set; }

    internal string Subject { get; private set; } = string.Empty;

    internal DateTime WhenSent { get; private set; }

    internal DateTime? WhenDeleted { get; private set; }

    internal IReadOnlyList<EmailAddress> EmailAddresses => this._emailAddresses;

    internal static Result<Email> Create(
        Guid emailId, Guid domainId, Guid subdomainId, string subject, DateTime whenSent, IReadOnlyList<EmailReceived.EmailAddress> emailAddresses)
    {
        var email = new Email();
        var @event = new EmailReceived(emailId, domainId, subdomainId, subject, whenSent, emailAddresses);
        email.RaiseEvent(@event);

        return Result.Ok(email);
    }

    internal Result Delete(DateTime whenDeleted)
    {
        if (this.WhenDeleted.HasValue)
        {
            return Result.Fail();
        }

        var @event = new EmailDeleted(whenDeleted);
        this.RaiseEvent(@event);

        return Result.Ok();
    }
}