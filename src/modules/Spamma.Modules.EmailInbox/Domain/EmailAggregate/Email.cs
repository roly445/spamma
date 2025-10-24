using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Client.Contracts;
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

    internal static Result<Email, BluQubeErrorData> Create(
        Guid emailId, Guid domainId, Guid subdomainId, string subject, DateTime whenSent, IReadOnlyList<EmailReceived.EmailAddress> emailAddresses)
    {
        var email = new Email();
        var @event = new EmailReceived(emailId, domainId, subdomainId, subject, whenSent, emailAddresses);
        email.RaiseEvent(@event);

        return Result.Ok<Email, BluQubeErrorData>(email);
    }

    internal ResultWithError<BluQubeErrorData> Delete(DateTime whenDeleted)
    {
        if (this.WhenDeleted.HasValue)
        {
            return ResultWithError.Fail<BluQubeErrorData>(new BluQubeErrorData(EmailInboxErrorCodes.EmailAlreadyDeleted, $"Email with ID '{this.Id}' has already been deleted."));
        }

        var @event = new EmailDeleted(whenDeleted);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }
}