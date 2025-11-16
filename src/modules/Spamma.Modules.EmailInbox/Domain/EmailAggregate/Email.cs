using System.Runtime.InteropServices.JavaScript;
using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

/// <summary>
/// Business logic for Email aggregate.
/// </summary>
internal partial class Email : AggregateRoot
{
    private readonly List<EmailAddress> _emailAddresses = new();
    private DateTime? _deletedAt;
    private Guid? _campaignId;

    private Email()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid DomainId { get; private set; }

    internal Guid SubdomainId { get; private set; }

    internal string Subject { get; private set; } = string.Empty;

    internal DateTimeOffset WhenSent { get; private set; }

    internal DateTime WhenDeleted => this._deletedAt ?? throw new InvalidOperationException("Email is not deleted. Check IsDeleted before accessing DeletedAt.");

    internal bool IsDeleted => this._deletedAt.HasValue;

    internal bool IsFavorite { get; private set; }

    internal Guid CampaignId => this._campaignId ?? throw new InvalidOperationException("CampaignId is not set. Check IsPartOfCampaign before accessing CampaignId.");

    internal bool IsPartOfCampaign => this._campaignId.HasValue;

    internal IReadOnlyList<EmailAddress> EmailAddresses => this._emailAddresses;

    internal static Result<Email, BluQubeErrorData> Create(
        Guid emailId, Guid domainId, Guid subdomainId, string subject, DateTimeOffset whenSent, IReadOnlyList<EmailReceived.EmailAddress> emailAddresses)
    {
        var email = new Email();
        var @event = new EmailReceived(emailId, domainId, subdomainId, subject, whenSent, emailAddresses);
        email.RaiseEvent(@event);

        return Result.Ok<Email, BluQubeErrorData>(email);
    }

    internal static Result<Email, BluQubeErrorData> Create(
        Guid emailId, Guid domainId, Guid subdomainId, string subject, DateTimeOffset whenSent, IReadOnlyList<EmailReceived.EmailAddress> emailAddresses, Guid campaignId)
    {
        var email = new Email();
        var @event = new EmailReceived(emailId, domainId, subdomainId, subject, whenSent, emailAddresses);
        email.RaiseEvent(@event);
        email.RaiseEvent(new CampaignCaptured(campaignId));

        return Result.Ok<Email, BluQubeErrorData>(email);
    }

    internal ResultWithError<BluQubeErrorData> Delete(DateTime whenDeleted)
    {
        if (this.IsDeleted)
        {
            return ResultWithError.Fail(new BluQubeErrorData(EmailInboxErrorCodes.EmailAlreadyDeleted, $"Email with ID '{this.Id}' has already been deleted."));
        }

        var @event = new EmailDeleted(whenDeleted);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> MarkAsFavorite(DateTime whenMarked)
    {
        if (this.IsDeleted)
        {
            return ResultWithError.Fail(new BluQubeErrorData(EmailInboxErrorCodes.EmailAlreadyDeleted, $"Cannot mark deleted email '{this.Id}' as favorite."));
        }

        if (this.IsFavorite)
        {
            return ResultWithError.Fail(new BluQubeErrorData(EmailInboxErrorCodes.EmailAlreadyFavorited, $"Email '{this.Id}' is already marked as favorite."));
        }

        var @event = new EmailMarkedAsFavorite(whenMarked);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> UnmarkAsFavorite(DateTime whenUnmarked)
    {
        if (this.IsDeleted)
        {
            return ResultWithError.Fail(new BluQubeErrorData(EmailInboxErrorCodes.EmailAlreadyDeleted, $"Cannot unmark deleted email '{this.Id}' as favorite."));
        }

        if (!this.IsFavorite)
        {
            return ResultWithError.Fail(new BluQubeErrorData(EmailInboxErrorCodes.EmailNotFavorited, $"Email '{this.Id}' is not marked as favorite."));
        }

        var @event = new EmailUnmarkedAsFavorite(whenUnmarked);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }
}