using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

internal class EmailBuilder
{
    private Guid _emailId = Guid.NewGuid();
    private Guid _domainId = Guid.NewGuid();
    private Guid _subdomainId = Guid.NewGuid();
    private string _subject = "Test Subject";
    private DateTime _whenSent = DateTime.UtcNow;
    private List<EmailReceived.EmailAddress> _emailAddresses = new()
    {
        new("test@example.com", "Test User", EmailAddressType.To),
    };

    private Guid? _campaignId = null;
    private bool _isFavorite = false;

    internal EmailBuilder WithId(Guid emailId)
    {
        this._emailId = emailId;
        return this;
    }

    internal EmailBuilder WithDomainId(Guid domainId)
    {
        this._domainId = domainId;
        return this;
    }

    internal EmailBuilder WithSubdomainId(Guid subdomainId)
    {
        this._subdomainId = subdomainId;
        return this;
    }

    internal EmailBuilder WithSubject(string subject)
    {
        this._subject = subject;
        return this;
    }

    internal EmailBuilder WithWhenSent(DateTime whenSent)
    {
        this._whenSent = whenSent;
        return this;
    }

    internal EmailBuilder WithEmailAddresses(List<EmailReceived.EmailAddress> emailAddresses)
    {
        this._emailAddresses = emailAddresses;
        return this;
    }

    internal EmailBuilder WithCampaignId(Guid? campaignId)
    {
        this._campaignId = campaignId;
        return this;
    }

    internal EmailBuilder WithIsFavorite(bool isFavorite)
    {
        this._isFavorite = isFavorite;
        return this;
    }

    internal Email Build()
    {
        var result = Email.Create(
            this._emailId,
            this._domainId,
            this._subdomainId,
            this._subject,
            this._whenSent,
            this._emailAddresses);

        var email = result.Value;

        // Apply campaign ID directly (it's a settable property)
        if (this._campaignId.HasValue)
        {
            email.CampaignId = this._campaignId.Value;
        }

        // Apply favorite status if set
        if (this._isFavorite)
        {
            var favoriteResult = email.MarkAsFavorite(DateTime.UtcNow);
            if (favoriteResult.IsFailure)
            {
                throw new InvalidOperationException($"Failed to mark as favorite: {favoriteResult.Error}");
            }
        }

        return email;
    }
}
